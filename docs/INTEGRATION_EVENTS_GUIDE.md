# Integration Events Setup Guide

This guide walks you through the steps to add and configure a new integration event in the DopamineKick.API project.

## Overview

The project uses an **Outbox Pattern** with **RabbitMQ** for reliable event publishing and consumption. Integration events are published asynchronously via a background service and consumed by other modules through dedicated consumer services.

### Architecture Flow

1. Command Handler → Creates OutboxMessage → Saves to DB
2. OutboxPublisher (background service) → Polls DB → Publishes to Mediator
3. IntegrationEventPublishHandler → Publishes to RabbitMQ
4. RabbitMQ → Routes to Queue(s)
5. Consumer Service → Delegates to Handler → Processes Event → Acknowledges

### Recommended Pattern: IIntegrationEventHandler<TEvent>

This project uses a **clean architecture pattern** for consuming integration events with proper layer separation:

**✅ Application Layer (Business Logic)**
- Create handlers implementing `IIntegrationEventHandler<TEvent>`
- Contains all business logic for processing events
- Can inject domain services, repositories, and other dependencies
- Easy to unit test in isolation

**✅ Infrastructure Layer (Subscription Management)**
- Consumer service (BackgroundService) manages RabbitMQ subscription
- Injects and delegates to the handler
- Focuses only on infrastructure concerns (queues, routing, retry)

**✅ Benefits:**
- **Separation of Concerns** - Business logic separated from infrastructure
- **Testability** - Handler can be unit tested without RabbitMQ
- **Dependency Injection** - Handler gets only the dependencies it needs
- **Scalability** - Easy to add multiple handlers for different events
- **Clean Architecture** - Follows dependency rule (Infrastructure → Application → Abstractions)

**Example:**
```csharp
// Application Layer - HabitReminderCreatedHandler.cs
public class HabitReminderCreatedHandler : IIntegrationEventHandler<HabitReminderCreated>
{
    public async Task HandleAsync(HabitReminderCreated @event, CancellationToken ct)
    {
        // Business logic here
    }
}

// Infrastructure Layer - HabitReminderConsumerService.cs
public class HabitReminderConsumerService(
    IMessageConsumer consumer,
    IIntegrationEventHandler<HabitReminderCreated> handler) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await consumer.Subscribe<HabitReminderCreated>(
            queueName: "notifications.habit-reminders",
            routingKey: MessagingConstants.HabitReminderCreatedKey,
            handler: async @event => await handler.HandleAsync(@event, stoppingToken));
    }
}
```

See Step 9 for detailed implementation instructions.

---

## Step 1: Define the Integration Event

Create a new event class that inherits from `IntegrationEvent` and implements `INotification`.

**Location:** `src/Shared/Common.IntegrationEvents/{ModuleName}/`

**Example:**

```csharp
using Common.Abstractions.Messaging;
using MediatR;

namespace Common.IntegrationEvents.YourModule;

[IntegrationEventRoutingKey(MessagingConstants.YourEventRoutingKey)]
public record YourEventCreated(
    Guid MessageId,
    Guid EntityId,
    Guid UserId,
    string SomeProperty) : IntegrationEvent, INotification;
```

**Key Requirements:**
- Inherit from `IntegrationEvent` base class
- Implement `INotification` from MediatR
- Use `[IntegrationEventRoutingKey]` attribute with your routing key constant
- Define as a `record` for immutability
- Include `MessageId` as first parameter (used for idempotency)
- Include all data needed by consumers

---

## Step 2: Add Routing Key Constant

Add your routing key to the messaging constants.

**File:** `src/Shared/Common.Abstractions/Messaging/MessagingConstants.cs`

```csharp
public static class MessagingConstants
{
    // ... existing constants ...

    // Routing Keys
    public const string YourEventRoutingKey = "your.module.event.created";

    // Queue Names (if needed)
    public const string YourModuleQueue = "your-module-queue";
}
```

**Naming Convention:**
- Use dot notation: `{module}.{entity}.{action}`
- Example: `habit.reminder.created`, `user.profile.updated`

---

## Step 3: Create Outbox Message in Command Handler

In your command handler, create an `OutboxMessage` alongside your domain entity within the same transaction.

**Location:** `src/Modules/{YourModule}/{YourModule}.Application/{Entity}/Commands/`

**Example:**

```csharp
using System.Text.Json;
using Common.IntegrationEvents.YourModule;
using YourModule.Domain.Entities;

public class YourCommandHandler : ICommandHandler<YourCommand, Result<Guid>>
{
    private readonly IYourRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Guid>> Handle(
        YourCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Create your domain entity
        var entity = new YourEntity
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            // ... other properties
        };

        await _repository.CreateAsync(entity, cancellationToken);

        // 2. Create the integration event
        var messageId = Guid.NewGuid();
        var integrationEvent = new YourEventCreated(
            messageId,
            entity.Id,
            entity.UserId,
            entity.SomeProperty
        );

        // 3. Create outbox message
        var outboxMessage = new OutboxMessage
        {
            MessageId = messageId,
            Type = typeof(YourEventCreated).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(integrationEvent),
            Published = false
        };

        await _repository.CreateOutboxMessageAsync(outboxMessage, cancellationToken);

        // 4. Save both in same transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);
    }
}
```

**Key Points:**
- Generate unique `MessageId` for idempotency tracking
- Use `AssemblyQualifiedName` for type serialization
- Set `Published = false` initially
- Save entity and outbox message in same transaction

---

## Step 4: Ensure OutboxMessage Entity Exists

If your module doesn't have the `OutboxMessage` entity, create it.

**Location:** `src/Modules/{YourModule}/{YourModule}.Domain/Entities/OutboxMessage.cs`

```csharp
using Common.Abstractions.Entities;

namespace YourModule.Domain.Entities;

public class OutboxMessage : IAuditable
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool Published { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
```

---

## Step 5: Configure OutboxMessage Entity

Create EF Core configuration for the `OutboxMessage` table.

**Location:** `src/Modules/{YourModule}/{YourModule}.Infrastructure/Configuration/OutboxMessageConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YourModule.Domain.Entities;

namespace YourModule.Infrastructure.Configuration;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MessageId)
            .IsRequired();

        builder.HasIndex(x => x.MessageId)
            .IsUnique();

        builder.Property(x => x.Type)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Payload)
            .IsRequired();

        builder.Property(x => x.Published)
            .IsRequired();

        builder.HasIndex(x => new { x.Published, x.CreatedAt });
    }
}
```

**Apply configuration in DbContext:**

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    // ... other configurations
}
```

---

## Step 6: Add Repository Method

Add a method to create outbox messages in your repository.

**Interface:** `src/Modules/{YourModule}/{YourModule}.Application/Abstractions/IYourRepository.cs`

```csharp
Task CreateOutboxMessageAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken);
```

**Implementation:** `src/Modules/{YourModule}/{YourModule}.Infrastructure/Repositories/YourRepository.cs`

```csharp
public async Task CreateOutboxMessageAsync(
    OutboxMessage outboxMessage,
    CancellationToken cancellationToken)
{
    await _context.Set<OutboxMessage>().AddAsync(outboxMessage, cancellationToken);
}
```

---

## Step 7: Create OutboxPublisher Background Service

If your module doesn't have one, create an `OutboxPublisher` background service.

**Location:** `src/Modules/{YourModule}/{YourModule}.Infrastructure/BackgroundServices/OutboxPublisher.cs`

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YourModule.Application.Abstractions;
using YourModule.Domain.Entities;

namespace YourModule.Infrastructure.BackgroundServices;

public class OutboxPublisher(
    IServiceProvider serviceProvider,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private const int DelayMs = 5000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxPublisher background service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(DelayMs, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IYourRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var messages = await repository.GetUnpublishedOutboxMessagesAsync(
            BatchSize,
            cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type);
                if (eventType == null)
                {
                    logger.LogError(
                        "Could not resolve type {Type} for outbox message {MessageId}",
                        message.Type,
                        message.MessageId);
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Payload, eventType);
                if (@event == null)
                {
                    logger.LogError(
                        "Could not deserialize payload for outbox message {MessageId}",
                        message.MessageId);
                    continue;
                }

                await mediator.Publish(@event, cancellationToken);

                message.Published = true;
                message.PublishedAt = DateTimeOffset.UtcNow;

                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Published outbox message {MessageId} of type {EventType}",
                    message.MessageId,
                    eventType.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error publishing outbox message {MessageId}",
                    message.MessageId);
            }
        }
    }
}
```

**Add repository method:**

```csharp
// Interface
Task<List<OutboxMessage>> GetUnpublishedOutboxMessagesAsync(
    int batchSize,
    CancellationToken cancellationToken);

// Implementation
public async Task<List<OutboxMessage>> GetUnpublishedOutboxMessagesAsync(
    int batchSize,
    CancellationToken cancellationToken)
{
    return await _context.Set<OutboxMessage>()
        .Where(x => !x.Published)
        .OrderBy(x => x.CreatedAt)
        .Take(batchSize)
        .ToListAsync(cancellationToken);
}
```

---

## Step 8: Register OutboxPublisher

Register the OutboxPublisher as a hosted service in your module's infrastructure registration.

**File:** `src/Modules/{YourModule}/{YourModule}.Infrastructure/DependencyInjection.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using YourModule.Infrastructure.BackgroundServices;

public static class DependencyInjection
{
    public static IServiceCollection AddYourModuleInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ... other registrations ...

        // Register OutboxPublisher
        services.AddHostedService<OutboxPublisher>();

        return services;
    }
}
```

---

## Step 9: Create Integration Event Handler (Application Layer)

**Recommended Approach:** Separate business logic from infrastructure by creating a handler in the Application layer.

### 9.1: Create the Handler Interface Implementation

**Location:** `src/Modules/{TargetModule}/{TargetModule}.Application/Handlers/YourEventCreatedHandler.cs`

```csharp
using Common.Abstractions.Messaging;
using Common.IntegrationEvents.YourModule;
using Microsoft.Extensions.Logging;

namespace TargetModule.Application.Handlers;

public class YourEventCreatedHandler(
    ILogger<YourEventCreatedHandler> logger
    // Inject other dependencies as needed (repositories, services, etc.)
) : IIntegrationEventHandler<YourEventCreated>
{
    public async Task HandleAsync(YourEventCreated @event, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Processing YourEventCreated: MessageId={MessageId}, EntityId={EntityId}",
            @event.MessageId,
            @event.EntityId);

        try
        {
            // TODO: Implement your business logic here
            // Example: Store notification, send email, update cache, etc.

            await Task.CompletedTask;

            logger.LogInformation(
                "Successfully processed YourEventCreated: MessageId={MessageId}",
                @event.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error processing YourEventCreated: MessageId={MessageId}",
                @event.MessageId);
            throw; // Re-throw to trigger retry mechanism
        }
    }
}
```

### 9.2: Create Consumer Service (Infrastructure Layer)

The consumer service focuses solely on subscription management and delegates to the handler.

**Location:** `src/Modules/{TargetModule}/{TargetModule}.Infrastructure/BackgroundServices/YourEventConsumerService.cs`

```csharp
using Common.Abstractions.Messaging;
using Common.IntegrationEvents.YourModule;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TargetModule.Infrastructure.BackgroundServices;

public class YourEventConsumerService(
    IMessageConsumer consumer,
    IIntegrationEventHandler<YourEventCreated> handler,
    ILogger<YourEventConsumerService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("YourEvent Consumer Service starting");

        await consumer.Subscribe<YourEventCreated>(
            queueName: "target-module.your-event-queue",
            routingKey: MessagingConstants.YourEventRoutingKey,
            handler: async @event => await handler.HandleAsync(@event, stoppingToken));
    }
}
```

**Architecture Benefits:**

✅ **Separation of Concerns** - Business logic separated from infrastructure
✅ **Testability** - Handler can be unit tested independently
✅ **Dependency Injection** - Handler only gets dependencies it needs
✅ **Scalability** - Easy to add more handlers for different event types
✅ **Clean Architecture** - Follows dependency rule (Infrastructure → Application → Abstractions)

**Key Points:**
- Handler lives in Application layer (business logic)
- Consumer service lives in Infrastructure layer (subscription management)
- Queue name should be descriptive: `{module}.{purpose}`
- Use the routing key defined in `MessagingConstants`
- Handle exceptions properly (throw to trigger retry, or catch to prevent DLQ)
- Log important information for debugging

---

## Step 10: Register Handler and Consumer Service

### 10.1: Register the Handler (Application Layer)

**File:** `src/Modules/{TargetModule}/{TargetModule}.Application/DependencyInjection.cs`

```csharp
using Common.Abstractions.Messaging;
using Common.IntegrationEvents.YourModule;
using Microsoft.Extensions.DependencyInjection;
using TargetModule.Application.Handlers;

namespace TargetModule.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTargetModuleApplication(
        this IServiceCollection services)
    {
        // ... other registrations ...

        // Register Integration Event Handler
        services.AddScoped<IIntegrationEventHandler<YourEventCreated>, YourEventCreatedHandler>();

        return services;
    }
}
```

### 10.2: Register the Consumer Service (Infrastructure Layer)

**File:** `src/Modules/{TargetModule}/{TargetModule}.Infrastructure/DependencyInjection.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using TargetModule.Infrastructure.BackgroundServices;

namespace TargetModule.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTargetModuleInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ... other registrations ...

        // Register Consumer Service
        services.AddHostedService<YourEventConsumerService>();

        return services;
    }
}
```

**Important Notes:**
- Handler is registered as **Scoped** - a new instance is created for each message processing scope
- Consumer service is registered as **HostedService** - singleton background service
- The handler gets resolved from the service provider when processing each message

---

## Step 11: Run Database Migration

If you added the `OutboxMessage` entity for the first time, create and apply a migration.

```bash
# Navigate to the Infrastructure project
cd src/Modules/{YourModule}/{YourModule}.Infrastructure

# Create migration
dotnet ef migrations add AddOutboxMessage --startup-project ../../../Host/WebApi

# Apply migration
dotnet ef database update --startup-project ../../../Host/WebApi
```

---

## Step 12: Verify RabbitMQ Configuration

Ensure RabbitMQ is properly configured in `appsettings.json`.

**File:** `src/Host/WebApi/appsettings.json`

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "MaxRetryCount": 3,
    "RetryDelayMs": 5000
  }
}
```

---

## Testing Your Integration Event

### 1. Start RabbitMQ

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Access RabbitMQ Management UI: `http://localhost:15672` (guest/guest)

### 2. Run the Application

```bash
dotnet run --project src/Host/WebApi
```

### 3. Trigger the Event

Call the API endpoint that triggers your command handler.

### 4. Verify in Logs

Check that:
1. OutboxMessage was created
2. OutboxPublisher picked up the message
3. Event was published to RabbitMQ
4. Consumer service received and processed the event

### 5. Check RabbitMQ UI

- Verify exchange `habits-direct` exists
- Verify your queue exists and is bound to the exchange
- Check message counts (published/consumed)
- Inspect dead letter queue if messages failed

---

## Error Handling & Retry Behavior

### Automatic Retry
- Consumer automatically retries failed messages up to `MaxRetryCount` (default: 3)
- Retry delay: `RetryDelayMs` (default: 5000ms)
- Retry count tracked via `x-retry-count` header

### Dead Letter Queue (DLQ)
Messages are sent to DLQ when:
- Max retries exceeded
- Deserialization fails
- Message is null

**DLQ naming:** `{queue-name}.dlq`

### Manual DLQ Processing
Check DLQ in RabbitMQ Management UI and manually republish or investigate failures.

---

## Common Issues & Solutions

### Issue: Type not found during deserialization
**Solution:** Ensure `AssemblyQualifiedName` is used when storing the Type in OutboxMessage.

### Issue: Messages stuck in outbox
**Solution:** Check OutboxPublisher logs. Verify Mediator handler is registered.

### Issue: Consumer not receiving messages
**Solution:**
- Verify queue binding in RabbitMQ UI
- Check routing key matches between publisher and consumer
- Ensure consumer service is registered as HostedService

### Issue: Messages going to DLQ immediately
**Solution:** Check consumer handler for exceptions. Review deserialization logic.

---

## Best Practices

1. **Idempotency:** Always include `MessageId` and implement idempotency checks in consumers
2. **Small Events:** Keep events small and focused; include IDs rather than entire entities
3. **Versioning:** Plan for event schema evolution (consider versioned event types)
4. **Logging:** Log important state transitions for debugging
5. **Monitoring:** Monitor OutboxMessage table growth and DLQ message counts
6. **Testing:** Write integration tests for publish/consume workflows
7. **Transactions:** Always save entity and outbox message in same transaction
8. **Error Handling:** Only throw exceptions in consumers when retry is appropriate

---

## Architecture Components Reference

### Core Infrastructure Files
- **IntegrationEvent base:** `src/Shared/Common.Abstractions/Messaging/IntegrationEvent.cs`
- **Routing attribute:** `src/Shared/Common.Abstractions/Messaging/IntegrationEventRoutingKeyAttribute.cs`
- **Event handler interface:** `src/Shared/Common.Abstractions/Messaging/IIntegrationEventHandler.cs`
- **Publisher interface:** `src/Shared/Common.Abstractions/Messaging/IMessagePublisher.cs`
- **Consumer interface:** `src/Shared/Common.Abstractions/Messaging/IMessageConsumer.cs`
- **Publish handler:** `src/Shared/Common.Infrastructure/Messaging/EventPublishHandler.cs`
- **RabbitMQ publisher:** `src/Shared/Common.Infrastructure/Messaging/RabbitMqPublisher.cs`
- **RabbitMQ consumer:** `src/Shared/Common.Infrastructure/Messaging/RabbitMqConsumer.cs`
- **RabbitMQ options:** `src/Shared/Common.Infrastructure/Messaging/RabbitMqOptions.cs`
- **Connection factory:** `src/Shared/Common.Infrastructure/Messaging/RabbitMqConnectionFactory.cs`

### Example Implementation
- **Event:** `src/Shared/Common.IntegrationEvents/Habits/HabitReminderCreated.cs`
- **Command handler:** `src/Modules/Habits/Habits.Application/Habits/Commands/CreateHabitReminder.cs`
- **OutboxPublisher:** `src/Modules/Habits/Habits.Infrastructure/BackgroundServices/OutboxPublisher.cs`
- **Event Handler:** `src/Modules/Notifications/Notifications.Application/Handlers/HabitReminderCreatedHandler.cs`
- **Consumer Service:** `src/Modules/Notifications/Notifications.Infrastructure/BackgroundServices/HabitReminderConsumerService.cs`

---

## Summary Checklist

**Publishing Side (Source Module):**
- [ ] Define integration event with `[IntegrationEventRoutingKey]` attribute
- [ ] Add routing key constant to `MessagingConstants`
- [ ] Create `OutboxMessage` in command handler
- [ ] Configure `OutboxMessage` entity in EF Core
- [ ] Add repository method for outbox messages
- [ ] Create/verify `OutboxPublisher` background service exists
- [ ] Register `OutboxPublisher` as hosted service
- [ ] Run database migration if needed

**Consuming Side (Target Module):**
- [ ] Create event handler in Application layer implementing `IIntegrationEventHandler<TEvent>`
- [ ] Register handler as scoped service in Application DI
- [ ] Create consumer service in Infrastructure layer
- [ ] Inject handler into consumer service
- [ ] Register consumer service as hosted service in Infrastructure DI
- [ ] Ensure Application project references Common.IntegrationEvents
- [ ] Ensure Infrastructure project references Application

**Testing & Verification:**
- [ ] Verify RabbitMQ configuration in appsettings.json
- [ ] Start RabbitMQ (Docker or local)
- [ ] Test end-to-end flow
- [ ] Monitor logs for both publishing and consuming
- [ ] Check RabbitMQ Management UI for queue bindings
- [ ] Verify messages are consumed and acknowledged
- [ ] Test error scenarios and DLQ behavior

---

*Generated for DopamineKick.API - A modular monolith with reliable event-driven architecture*
