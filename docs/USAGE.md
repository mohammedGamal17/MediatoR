## DOCS
### ⚙️ Usage

#### Example Request & Handler

```csharp
public class GetUserQuery : IRequest<UserDto> { }

public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new UserDto { Name = "John Doe" });
    }
}
```
---
#### Example Notification & Handler

```csharp
public class UserCreatedNotification : INotification { }

public class UserCreatedHandler : INotificationHandler<UserCreatedNotification>
{
    public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        Console.WriteLine("User created event handled.");
        return Task.CompletedTask;
    }
}
```
---
#### Example Mediator Usage

```csharp
var result = await mediator.Send(new GetUserQuery());
await mediator.Publish(new UserCreatedNotification());
```
---
#### Configuration
- 🚀 Register MediatR in your DI container:
```csharp
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
```
