namespace MoMediatoR
{
    /// <summary>
    /// Defines a pipeline behavior that wraps around the execution of a request handler.
    /// Pipeline behaviors can be used to implement cross-cutting concerns such as validation, logging,
    /// performance monitoring, authorization, and exception handling.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request being handled.</typeparam>
    /// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
    public interface IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// Handles the incoming request and delegates control to the next element in the pipeline.
        /// </summary>
        /// <param name="request">The request instance.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="next">A delegate to invoke the next behavior or handler in the pipeline.</param>
        /// <returns>A task that represents the asynchronous operation, returning the response.</returns>
        Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken = default);
    }

    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
}
