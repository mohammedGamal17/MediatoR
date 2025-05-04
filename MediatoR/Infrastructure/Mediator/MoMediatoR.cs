namespace MoMediatoR
{
    /// <summary>
    /// Represents a custom implementation of the <see cref="IMoMediatoR"/> interface,
    /// facilitating the sending of requests and publishing of notifications.
    /// </summary>
    public class MoMediatoR : IMoMediatoR
    {
        #region Fields
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScopeFactory _scopeFactory;

        // Primary cache for compiled delegates
        private readonly ConcurrentDictionary<Type, Func<object, object, Task<object>>> _handlerInvokers;
        private readonly ConcurrentDictionary<Type, List<(Type HandlerType, Func<object, INotification, CancellationToken, Task> Delegate)>> _notificationInvokers;
        private readonly ConcurrentDictionary<Type, Type> _handlerTypes;

        private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object>>> _pipelineExecutors;
        private readonly MoMediatoROptions _options;

        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MoMediatoR"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve request and notification handlers.</param>
        /// <param name="handlerInvokers">A dictionary that caches compiled delegates for handling requests.</param>
        /// <param name="notificationInvokers">A dictionary that caches delegates for handling notifications.</param>
        /// <param name="handlerTypes">A dictionary that maps request types to their corresponding handler types.</param>
        public MoMediatoR(
            IServiceProvider serviceProvider,
            ConcurrentDictionary<Type, Func<object, object, Task<object>>> handlerInvokers,
            ConcurrentDictionary<Type, List<(Type HandlerType, Func<object, INotification, CancellationToken, Task>)>> notificationInvokers,
            ConcurrentDictionary<Type, Type> handlerTypes,
            IServiceScopeFactory scopeFactory,
            MoMediatoROptions options,
            ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object>>> pipelineExecutors)
        {
            _serviceProvider = serviceProvider;
            _handlerInvokers = handlerInvokers;
            _notificationInvokers = notificationInvokers;
            _handlerTypes = handlerTypes;
            _scopeFactory = scopeFactory;
            _options = options;
            _pipelineExecutors = pipelineExecutors;
        }
        #endregion

        #region Methods

        #region Public
        /// <summary>
        /// Sends a request of type <typeparamref name="TResponse"/> and returns the response asynchronously.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response expected from handling the request.</typeparam>
        /// <param name="request">The request to be handled.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation, with the response of type <typeparamref name="TResponse"/>.</returns>
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var requestType = request.GetType();

            if (!_pipelineExecutors.TryGetValue(requestType, out var executor))
            {
                executor = BuildPipelineExecutor<TResponse>(requestType);
                _pipelineExecutors.TryAdd(requestType, executor);
            }

            var result = await executor(_serviceProvider, request, cancellationToken);
            return (TResponse)result!;
        }

        /// <summary>
        /// Publishes a notification of type <typeparamref name="TNotification"/> to all registered handlers asynchronously.
        /// </summary>
        /// <typeparam name="TNotification">The type of the notification to be published.</typeparam>
        /// <param name="notification">The notification to be handled by the registered handlers.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            var notificationType = notification.GetType();

            if (_notificationInvokers.TryGetValue(notificationType, out var handlerEntries))
            {
                using var scope = _scopeFactory.CreateScope();

                foreach (var (handlerType, handlerDelegate) in handlerEntries)
                {
                    var handlerInstance = scope.ServiceProvider.GetRequiredService(handlerType);

                    try
                    {
                        await handlerDelegate(handlerInstance, notification, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException($"Failed to execute Notification handler for {handlerType.Name}", ex);
                    }
                }
            }

        }
        #endregion

        #region Private
        private Func<IServiceProvider, object, CancellationToken, Task<object>> BuildPipelineExecutor<TResponse>(Type requestType)
        {
            if (!_handlerTypes.TryGetValue(requestType, out var handlerType))
                throw new InvalidOperationException($"No handler for {requestType.Name}");

            if (!_handlerInvokers.TryGetValue(requestType, out var handlerInvoker))
                throw new InvalidOperationException($"No compiled delegate for {requestType.Name}");

            var method = typeof(MoMediatoR)
                .GetMethod(nameof(BuildPipelineExecutorGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(requestType, typeof(TResponse));

            return (Func<IServiceProvider, object, CancellationToken, Task<object>>)method.Invoke(this, new object[] { handlerType, handlerInvoker })!;
        }
        private async Task<TResponse> ExecuteBehaviorPipeline<TRequest, TResponse>(IList<IPipelineBehavior<TRequest, TResponse>> behaviors, TRequest request, CancellationToken token, RequestHandlerDelegate<TResponse> finalHandler)
            where TRequest : IRequest<TResponse>
        {
            RequestHandlerDelegate<TResponse> current = finalHandler;

            for (int i = behaviors.Count - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var next = current;
                current = () => behavior.Handle(request, () => next(), token);
            }
            return await current();
        }     
        private Func<IServiceProvider, object, CancellationToken, Task<object>> BuildPipelineExecutorGeneric<TRequest, TResponse>(Type handlerType, Func<object, object, Task<object>> handlerInvoker)
            where TRequest : IRequest<TResponse>
        {
            return (sp, requestObj, token) =>
            {
                var scope = sp.CreateScope();
                var scopedProvider = scope.ServiceProvider;
                var handler = scopedProvider.GetRequiredService(handlerType);
                var request = (TRequest)requestObj;

                RequestHandlerDelegate<TResponse> handlerDelegate = () => handlerInvoker(handler, request)
                    .ContinueWith(t => (TResponse)t.Result!, token);

                var pipelineTypes = _options.GlobalPipelineBehaviors
                    .Select(t => t.IsGenericTypeDefinition ? t.MakeGenericType(typeof(TRequest), typeof(TResponse)) : t)
                    .ToList();

                var behaviors = pipelineTypes
                    .Select(bt => (IPipelineBehavior<TRequest, TResponse>)scopedProvider.GetRequiredService(bt))
                    .ToList();

                Func<Task<TResponse>> pipeline = () =>
                {
                    return ExecuteBehaviorPipeline(behaviors, request, token, handlerDelegate);
                };

                return pipeline().ContinueWith(t => (object)t.Result!, token);
            };
        }
        #endregion

        #endregion
    }
}
