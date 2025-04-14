using Microsoft.Extensions.DependencyInjection;

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
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Mediator"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve request and notification handlers.</param>
        public MoMediatoR(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sends a request of type <typeparamref name="TResponse"/> and returns the response asynchronously.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response expected from handling the request.</typeparam>
        /// <param name="request">The request to be handled.</param>
        /// <returns>A task representing the asynchronous operation, with the response of type <typeparamref name="TResponse"/>.</returns>
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
            var handler = _serviceProvider.GetService(handlerType);

            if (handler is null)
                throw new InvalidOperationException($"No handler registered for request type '{request.GetType().Name}'");


            var handleMethod = handlerType.GetMethod("Handle");
            if (handleMethod is null)
                throw new InvalidOperationException($"Handler '{handler.GetType().Name}' does not contain a 'Handle' method.");


            var result = handleMethod.Invoke(handler, new object[] { request, CancellationToken.None });
            if (result is not Task<TResponse> task)
                throw new InvalidOperationException($"Expected return type 'Task<{typeof(TResponse).Name}>' but got '{result?.GetType().Name ?? "null"}'.");


            return await task;
        }

        /// <summary>
        /// Publishes a notification of type <typeparamref name="TNotification"/> to all registered handlers asynchronously.
        /// </summary>
        /// <typeparam name="TNotification">The type of the notification to be published.</typeparam>
        /// <param name="notification">The notification to be handled by the registered handlers.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Publish<TNotification>(TNotification notification) where TNotification : INotification
        {
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(typeof(TNotification));
            var handlers = (IEnumerable<object>)_serviceProvider.GetServices(handlerType) ?? Enumerable.Empty<object>();

            foreach (var handler in handlers)
            {
                var method = handler.GetType().GetMethod("Handle");
                if (method is null)
                    throw new InvalidOperationException($"Handle method not found in {handler.GetType().Name}");

                var result = method.Invoke(handler, new object[] { notification, CancellationToken.None });

                if (result is not Task task)
                    throw new InvalidOperationException($"Handler method for {typeof(TNotification).Name} did not return a Task.");

                await task;
            }
        }
        #endregion

    }

}
