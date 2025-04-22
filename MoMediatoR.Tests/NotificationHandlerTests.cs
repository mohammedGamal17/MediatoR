using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MoMediatoR.Tests
{
    public class PingNotification : INotification
    {
        public string Message { get; set; } = "Ping!";
    }

    public class PingNotificationHandler : INotificationHandler<PingNotification>
    {
        public Task Handle(PingNotification notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Handled Notification: {notification.Message}");
            return Task.CompletedTask;
        }
    }


    public class NotificationHandlerTests
    {
        private readonly IMoMediatoR _mediator;

        public NotificationHandlerTests()
        {
            var services = new ServiceCollection();
            services.AddMoMediatoR(typeof(PingNotificationHandler).Assembly); // مهم جداً
            _mediator = services.BuildServiceProvider().GetRequiredService<IMoMediatoR>();
        }

        [Fact]
        public async Task NotificationHandler_ShouldBeInvoked()
        {
            // Arrange
            var notification = new PingNotification { Message = "Test notification" };

            await _mediator.Publish(notification);
        }
    }
}
