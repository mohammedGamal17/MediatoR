namespace MoMediatoR
{
    /// <summary>
    /// Defines the contract for a mediator that facilitates communication between different components 
    /// in a system by sending requests and publishing notifications.
    /// </summary>
    public interface IMoMediatoR
    {
        /// <summary>
        /// Sends a request to the appropriate handler and expects a response of type <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The type of the expected response.</typeparam>
        /// <param name="request">The request to be sent.</param>
        /// <returns>The response of type <typeparamref name="TResponse"/>.</returns>
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a notification to all registered handlers of type <typeparamref name="TNotification"/>.
        /// </summary>
        /// <typeparam name="TNotification">The type of the notification to be published.</typeparam>
        /// <param name="notification">The notification to be published.</param>
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
    }

}
