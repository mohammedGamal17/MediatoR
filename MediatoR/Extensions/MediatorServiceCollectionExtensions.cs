namespace MoMediatoR
{
    /// <summary>
    /// Extension methods for registering Mediator-related services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class MediatorServiceCollectionExtensions
    {
        #region Fields
        private static readonly ConcurrentDictionary<Assembly, Type[]> _handlerTypesCache = new();
        private static readonly ConcurrentDictionary<Type, Func<object, object, Task<object>>> _compiledHandlerDelegates = new();
        private static readonly ConcurrentDictionary<Type, List<(Type HandlerType, Func<object, INotification, CancellationToken, Task> Delegate)>> _compiledNotificationDelegates = new();
        private static readonly ConcurrentDictionary<Type, Type> _handlerTypeRegistry = new();
        private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object>>> _pipelineExecutors = new();
        #endregion

        #region Methods

        #region Public

        public static IServiceCollection AddMoMediatoR(this IServiceCollection services, params Assembly[] assemblies)
        {
            return AddMoMediatoR(services, _ => { }, assemblies);
        }

        /// <summary>
        /// Registers all request and notification handlers from the specified assemblies,
        /// as well as the Mediator service itself.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
        /// <param name="assemblies">The assemblies to scan for request and notification handlers. If no assemblies are provided, the calling assembly is used.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>

        public static IServiceCollection AddMoMediatoR(this IServiceCollection services, Action<MoMediatoROptions> configure, params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
                assemblies = new[] { Assembly.GetCallingAssembly() };

            var options = new MoMediatoROptions();
            configure?.Invoke(options);

            services.AddSingleton<IMoMediatoR>(sp =>
            {
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                return new MoMediatoR(sp, scopeFactory, options, _compiledHandlerDelegates, _compiledNotificationDelegates, _handlerTypeRegistry, _pipelineExecutors);
            });

            foreach (var behavior in options.GlobalPipelineBehaviors)
            {
                services.AddTransient(behavior);
            }

            foreach (var assembly in assemblies)
            {
                var types = _handlerTypesCache.GetOrAdd(assembly, a => a.GetTypes());

                // Request Handlers
                var requestHandlers = types.Where(t => t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)));

                foreach (var handler in requestHandlers)
                {
                    var interfaceType = handler.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
                    var requestType = interfaceType.GetGenericArguments()[0];
                    var responseType = interfaceType.GetGenericArguments()[1];

                    _handlerTypeRegistry.TryAdd(requestType, handler);

                    _compiledHandlerDelegates.GetOrAdd(requestType,
                        _ => CompileRequestHandler(handler, requestType, responseType));

                    services.AddTransient(handler);
                }

                // Notification Handlers
                var notificationHandlers = types.Where(t => t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)));

                foreach (var handler in notificationHandlers)
                {
                    var interfaceType = handler.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));
                    var notificationType = interfaceType.GetGenericArguments()[0];
                    var compiledDelegate = CompileNotificationHandler(handler, notificationType);

                    _compiledNotificationDelegates.AddOrUpdate(
                        notificationType,
                        _ => new List<(Type, Func<object, INotification, CancellationToken, Task>)> { (handler, compiledDelegate) },
                        (_, existingList) =>
                        {
                            var updatedList = new List<(Type, Func<object, INotification, CancellationToken, Task>)>(existingList)
                            {
                                (handler, compiledDelegate)
                            };
                            return updatedList;
                        });

                    services.AddTransient(handler);
                }
            }

            return services;
        }

        /// <summary>
        /// Registers all request and notification handlers from the assemblies of the specified marker types,
        /// as well as the Mediator service itself.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register services into.</param>
        /// <param name="markerTypes">An array of types used to determine the assemblies to scan for handlers.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMoMediatoR(this IServiceCollection services, params Type[] markerTypes)
        {
            var assemblies = markerTypes.Select(t => t.Assembly).ToArray();
            return AddMoMediatoR(services, assemblies);
        }
        #endregion

        #region Private
        /// <summary>
        /// Compiles a delegate for handling requests of a specific type.
        /// This delegate takes an object representing the handler and an object representing the request,
        /// and returns a task that resolves to an object representing the response.
        /// </summary>
        /// <param name="handlerType">The type of the request handler.</param>
        /// <param name="requestType">The type of the request to be handled.</param>
        /// <param name="responseType">The type of the response produced by handling the request.</param>
        /// <returns>A compiled delegate that can be used to invoke the request handler.</returns>
        private static Func<object, object, Task<object>> CompileRequestHandler(Type handlerType, Type requestType, Type responseType)
        {
            var method = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType).GetMethod("Handle")
                         ?? throw new InvalidOperationException("Could not find Handle method.");

            var handler = Expression.Parameter(typeof(object), "handler");
            var request = Expression.Parameter(typeof(object), "request");

            var castHandler = Expression.Convert(handler, handlerType);
            var castRequest = Expression.Convert(request, requestType);

            var cancellation = Expression.Constant(CancellationToken.None);

            var call = Expression.Call(castHandler, method, castRequest, cancellation);

            var convertCall = Expression.Call(
                typeof(TaskExtensions),
                nameof(TaskExtensions.ConvertTaskResult),
                new[] { responseType },
                call
            );

            var lambda = Expression.Lambda<Func<object, object, Task<object>>>(
                convertCall,
                handler,
                request
            );

            return lambda.Compile();
        }

        /// <summary>
        /// Compiles a delegate for handling notifications of a specific type.
        /// This delegate takes an object representing the handler, an INotification representing the notification,
        /// and a CancellationToken, returning a task that represents the asynchronous operation.
        /// </summary>
        /// <param name="handlerType">The type of the notification handler.</param>
        /// <param name="notificationType">The type of the notification to be handled.</param>
        /// <returns>A compiled delegate that can be used to invoke the notification handler.</returns>
        private static Func<object, INotification, CancellationToken, Task> CompileNotificationHandler(Type handlerType, Type notificationType)
        {
            var method = typeof(INotificationHandler<>).MakeGenericType(notificationType).GetMethod("Handle")
                         ?? throw new InvalidOperationException("Could not find Handle method.");

            var handler = Expression.Parameter(typeof(object), "handler");
            var notification = Expression.Parameter(typeof(INotification), "notification");
            var cancellationToken = Expression.Parameter(typeof(CancellationToken), "ct");

            var castHandler = Expression.Convert(handler, handlerType);
            var castNotification = Expression.Convert(notification, notificationType);

            var call = Expression.Call(castHandler, method, castNotification, cancellationToken);

            return Expression.Lambda<Func<object, INotification, CancellationToken, Task>>(call, handler, notification, cancellationToken).Compile();
        }
        #endregion

        #endregion
    }
}
