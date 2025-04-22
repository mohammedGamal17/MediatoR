using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MoMediatoR.Tests
{
    public class Ping : IRequest<string>
    {
        public string Message { get; set; } = "Ping!";
    }

    public class PingHandler : IRequestHandler<Ping, string>
    {
        public Task<string> Handle(Ping request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Pong: {request.Message}");
        }
    }

    public class RequestHandlerTests
    {
        private readonly IMoMediatoR _mediator;

        public RequestHandlerTests()
        {
            var services = new ServiceCollection();
            services.AddMoMediatoR(typeof(PingHandler).Assembly);
            _mediator = services.BuildServiceProvider().GetRequiredService<IMoMediatoR>();
        }

        [Fact]
        public async Task PingHandler_ReturnsExpectedResponse()
        {
            // Arrange
            var request = new Ping { Message = "Hello" };

            // Act
            var result = await _mediator.Send(request);

            // Assert
            Assert.Equal("Pong: Hello", result);
        }

        [Fact]
        public async Task CachedDelegates_AreUsedAcrossMultipleRequests()
        {
            // Arrange
            var request1 = new Ping { Message = "One" };
            var request2 = new Ping { Message = "Two" };

            // Act
            var result1 = await _mediator.Send(request1);
            var result2 = await _mediator.Send(request2);

            // Assert
            Assert.Equal("Pong: One", result1);
            Assert.Equal("Pong: Two", result2);
        }
    }
}
