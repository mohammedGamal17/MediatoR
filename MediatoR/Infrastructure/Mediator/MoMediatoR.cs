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

        // Primary cache for compiled delegates
        private readonly ConcurrentDictionary<Type, Func<object, object, Task<object>>> _handlerInvokers;
        private readonly ConcurrentDictionary<Type, List<Func<object, INotification, CancellationToken, Task>>> _notificationInvokers;
        private readonly ConcurrentDictionary<Type, Type> _handlerTypes;

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
            ConcurrentDictionary<Type, List<Func<object, INotification, CancellationToken, Task>>> notificationInvokers,
            ConcurrentDictionary<Type, Type> handlerTypes)
        {
            _serviceProvider = serviceProvider;
            _handlerInvokers = handlerInvokers;
            _notificationInvokers = notificationInvokers;
            _handlerTypes = handlerTypes;
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

            if (!_handlerInvokers.TryGetValue(requestType, out var invoker))
                throw new InvalidOperationException($"No handler registered for request type {requestType.FullName}");

            if (!_handlerTypes.TryGetValue(requestType, out var handlerType))
                throw new InvalidOperationException($"No handler implementation found for {requestType.FullName}");

            var handlerInstance = _serviceProvider.GetRequiredService(handlerType);

            try
            {
                var result = await invoker(handlerInstance, request);
                return (TResponse)result!;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to execute request handler for {requestType.Name}", ex);
            }
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

            if (_notificationInvokers.TryGetValue(notificationType, out var handlerDelegates))
            {
                foreach (var handlerDelegate in handlerDelegates)
                {
                    var handlerType = handlerDelegate.Method.DeclaringType!;
                    var handlerInstance = _serviceProvider.GetRequiredService(handlerType);

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

        #endregion

        #endregion
    }
}
