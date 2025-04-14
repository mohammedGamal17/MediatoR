# Mo Mediator Implementation (C#)

This project provides a clean, extensible implementation of the **Mediator pattern** in C#,customization, or lightweight usage.

---

## ⬇️ Download
- .NET CLI: dotnet add package MoMediator --version 1.0.5
- Package Manager: NuGet\Install-Package MoMediator -Version 1.0.5
- Package Reference: <PackageReference Include="MoMediator" Version="1.0.5" />

## ✨ Features

- ✅ Supports `Send<TResponse>` for request/response interactions.
- ✅ Supports `Publish<TNotification>` for broadcasting notifications.
- ✅ Reflection-based method invocation with validation.
- ✅ Clean code principles: null checks, proper error handling, and separation of concerns.

---

## 🧱 Architecture

This project uses the Mediator pattern to decouple sender and receiver components via two core interfaces:

- **IRequest<TResponse>** – Represents a command or query expecting a response.
- **INotification** – Represents a notification/event without an expected return.

Handlers are resolved using `IServiceProvider`, and their `Handle` methods are dynamically invoked using reflection.

---

## 🧩 Interfaces

```csharp
public interface IRequest<TResponse> { }

public interface INotification { }

public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface INotificationHandler<TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
```
---
## ⚙️ Usage

### Example Request & Handler

```csharp
using MoMediatoR;
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
### Example Notification & Handler

```csharp
using MoMediatoR;

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
### Example Mediator Usage

```csharp
using MoMediatoR;
private readonly IMoMediatoR _moMediatoR;

var result = await _moMediatoR.Send(new GetUserQuery());
await _moMediatoR.Publish(new UserCreatedNotification());
```
---
### Configuration
- 🚀 Register MediatR in your DI container:
```csharp
using MoMediatoR;
Services.AddMoMediatoR(typeof(Program).Assembly);
```
