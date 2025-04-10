namespace MediatoR.Core.Interfaces
{
    #region Contracts
    /// <summary>
    /// Defines the contract for a request handler that processes requests of type <typeparamref name="TRequest"/> 
    /// and returns a response of type <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request that this handler processes.</typeparam>
    /// <typeparam name="TResponse">The type of the response produced by handling the request.</typeparam>
    public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// Handles the specified request asynchronously and returns a response.
        /// </summary>
        /// <param name="request">The request to be handled.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation, containing the response of type <typeparamref name="TResponse"/>.</returns>
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }
    #endregion

    #region Markers
    /// <summary>
    /// Marker interface for requests that return a response of type <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response produced by handling the request.</typeparam>
    public interface IRequest<out TResponse> { }
    #endregion
}
