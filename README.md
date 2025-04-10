# Custom Mediator Implementation (C#)

This project provides a clean, extensible implementation of the **Mediator pattern** in C#, inspired by `MediatR` but built from scratch for learning, customization, or lightweight usage.

---

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
