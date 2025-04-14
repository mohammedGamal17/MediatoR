namespace MoMediatoR
{
    #region Contracts
    /// <summary>
    /// Defines the contract for a notification handler that processes notifications of type <typeparamref name="TNotification"/>.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification that this handler processes.</typeparam>
    public interface INotificationHandler<in TNotification> where TNotification : INotification
    {
        /// <summary>
        /// Handles the specified notification asynchronously.
        /// </summary>
        /// <param name="notification">The notification to be handled.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }
    #endregion

    #region Markers
    /// <summary>
    /// Marker interface for notifications.
    /// </summary>
    public interface INotification { }
    #endregion
}
