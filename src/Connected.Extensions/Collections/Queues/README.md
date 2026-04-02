# Queue System Architecture

## Overview

The Connected.Core queue system provides a robust, scalable framework for asynchronous message processing with features including priority-based ordering, automatic retry logic, debouncing, and long-running operation support.

## Core Concepts

### Message Flow

```
[QueueContext] → Enqueue → [Storage] → [Cache] → [QueueHost] → Dequeue → [Dispatcher] → [QueueJob] → [QueueAction]
```

1. **Enqueueing**: `QueueContext` creates messages with metadata and persists them to storage and cache
2. **Polling**: `QueueHost` periodically scans for visible messages and enqueues them to the dispatcher
3. **Processing**: `QueueJob` resolves the action handler and invokes it with the message payload
4. **Completion**: Successfully processed messages are deleted from storage and cache

### Key Components

#### QueueContext (Producer)
- Creates and enqueues messages for asynchronous processing
- Implements debouncing logic to prevent duplicate messages for the same group
- Configures message metadata: priority, expiration, visibility window, max dequeue count
- Validates messages before enqueueing based on group identifier

#### QueueAction (Consumer)
- Processes dequeued messages with access to the DTO payload
- Supports long-running operations through ping callbacks for visibility extension
- Receives cancellation tokens for graceful shutdown
- Should implement idempotent logic since messages may be retried

#### QueueHost (Orchestrator)
- Scheduled worker that polls the cache for visible messages
- Selects dequeue candidates based on priority, visibility, and dispatcher capacity
- Updates message state (NextVisible, PopReceipt, DequeueCount) during dequeue
- Cleans up expired messages and those exceeding max dequeue count
- Configurable polling interval and concurrent worker pool size

#### QueueJob (Processor)
- Manages the complete message processing lifecycle
- Extends visibility timeout before and periodically during processing
- Resolves action services from dependency injection
- Sets up ping callbacks for actions that need visibility extension
- Deletes messages upon successful completion

## Message Properties

| Property | Description | Default |
|----------|-------------|---------|
| **Created** | Timestamp when message was enqueued | Now |
| **Priority** | Order for dequeue selection (higher first) | 0 |
| **NextVisible** | Timestamp when message becomes visible | Now + 5s |
| **Expire** | Timestamp for automatic deletion | Now + 10m |
| **MaxDequeueCount** | Max retry attempts before deletion | 10 |
| **Group** | Identifier for debouncing (typically entity Id) | DTO.Id |
| **DebounceTimeout** | Time to allow new message with same group | 1 minute |
| **DequeueCount** | Number of times message has been dequeued | 0 |
| **PopReceipt** | Unique identifier assigned during dequeue | Generated |
| **Action** | Type of the action handler | typeof(TAction) |
| **Dto** | Serialized payload (JSON → binary) | Required |

## Debouncing Logic

Messages with the same **Action** type and **Group** identifier are subject to debouncing:

1. When enqueueing, check if a message with the same action+group exists
2. If exists and created within DebounceTimeout period → reject or update visibility
3. If DebounceTimeout elapsed → allow new message (queue may be stuck)
4. If visibility differs by >1 second → update existing message's NextVisible

This prevents queue flooding when multiple events trigger enqueueing for the same entity.

## Visibility Window Management

Messages use visibility windows to prevent duplicate processing:

- **Dequeue**: NextVisible = Now + 30s (default NextVisibleInterval)
- **Processing Start**: NextVisible = Now + 60s (initial guard)
- **Periodic Guard**: NextVisible = Now + 60s (every 20s during processing)
- **Ping Callback**: NextVisible = Now + requested duration (long-running operations)

If processing exceeds the visibility window, the message becomes visible again for automatic retry.

## Priority-Based Dequeue

Messages are dequeued in order:
1. **Priority** (descending) - higher priority first
2. **NextVisible** (ascending) - earlier visible time first
3. **Id** (ascending) - older messages first

The host optimizes selection by respecting the dispatcher's minimum priority (messages already queued).

## Concurrency Handling

Queue operations handle concurrency through:

- **Optimistic Locking**: Uses row version (ConcurrentEntity) for conflict detection
- **Retry Logic**: On conflict, refresh cache and retry with latest state
- **Isolation**: Queue storage uses isolated transactions (not shared DI scope)

## Usage Example

### Define DTO
```csharp
public record ProcessOrderDto : IPrimaryKeyDto<int>
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = default!;
}
```

### Implement Action
```csharp
public class ProcessOrderAction : QueueAction<ProcessOrderDto>
{
    protected override async Task OnInvoke()
    {
        // Long-running operation
        for (int i = 0; i < 10; i++)
        {
            await ProcessBatch(i);
            
            // Extend visibility every 30 seconds
            await Ping(TimeSpan.FromSeconds(60));
        }
    }
}
```

### Implement Context
```csharp
public class ProcessOrderContext(IStorageProvider storage, IQueueMessageCache cache)
    : QueueContext<QueueMessage, ProcessOrderAction, ProcessOrderDto>(storage, cache)
{
    // Optional: customize message metadata
    protected override int Priority => 10; // Higher priority
    protected override DateTimeOffset? Expire => DateTimeOffset.UtcNow.AddHours(1);
}
```

### Enqueue Message
```csharp
public class OrderService(IProcessOrderContext context)
{
    public async Task CompleteOrder(int orderId)
    {
        await context.Invoke(new ProcessOrderDto
        {
            Id = orderId,
            OrderNumber = $"ORD-{orderId}"
        });
    }
}
```

## Configuration

### Queue Host Settings
```csharp
public class MyQueueHost : QueueHost<MyQueueMessage, IMyQueueMessageCache>
{
    public MyQueueHost()
    {
        // Polling interval
        Timer = TimeSpan.FromMilliseconds(500);
        
        // Concurrent worker pool size
        QueueSize = 4;
        
        // Visibility timeout after dequeue
        NextVisibleInterval = TimeSpan.FromSeconds(30);
    }
}
```

### Dependency Injection Registration

Queue components are auto-registered through assembly scanning:

- **QueueContext**: Registered by interface (IQueueContext<TAction, TDto>)
- **QueueAction**: Registered as transient services
- **QueueHost**: Registered as hosted services (IHostedService)
- **QueueMessageCache**: Registered as entity cache

## Error Handling

### Automatic Retry
- Unhandled exceptions cause message to become visible again
- Message is retried up to MaxDequeueCount times
- After max attempts, message is deleted as poison

### Expiration Cleanup
- Host scans for messages where Expire ≤ Now
- Expired messages are deleted without processing

### Missing Action Types
- Messages with unresolvable action types are deleted
- Logged as errors for diagnostics

## Performance Considerations

### Cache Strategy
- Queue messages are cached with no expiration (TimeSpan.Zero)
- Cache refreshed on enqueue, dequeue, and update operations
- Bulk query returns all messages for efficient filtering

### Batching
- Host dequeues multiple messages per cycle (up to dispatcher capacity)
- Dispatcher processes messages concurrently across worker pool
- Priority-based selection optimizes important message handling

### Storage Isolation
- Queue storage uses isolated transactions (not DI scope)
- Changes commit immediately without affecting business transactions
- Prevents queue operations from blocking or being blocked by business logic

## Monitoring and Diagnostics

### Logging
- Action registration failures logged at Error level
- Message processing exceptions logged with stack traces
- Pop receipt lookup failures logged as warnings

### Metrics
- DequeueCount tracks retry attempts per message
- Priority distribution visible through MinPriority property
- Dispatcher.Available indicates current processing capacity

## Best Practices

1. **Idempotency**: Actions should be idempotent since messages may be retried
2. **Visibility Extension**: Call Ping() every 20-30 seconds for long operations
3. **Group Identifiers**: Use stable entity identifiers for debouncing
4. **Priority Assignment**: Reserve high priorities for time-sensitive operations
5. **Expiration Windows**: Set realistic expiration based on message relevance lifetime
6. **Max Dequeue Count**: Balance between retry attempts and poison message detection
7. **Error Handling**: Log errors but allow propagation for automatic retry
8. **DTO Size**: Keep DTOs compact; store only identifiers, not full entities
9. **Concurrency**: Accept optimistic locking conflicts; cache refresh handles them
10. **Testing**: Test with multiple hosts to verify concurrency and dequeue logic

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      Queue System                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐         ┌──────────────┐                 │
│  │ QueueContext │────────▶│   Storage    │                 │
│  │  (Enqueue)   │         │  IQueueMsg   │                 │
│  └──────────────┘         └──────┬───────┘                 │
│         │                         │                         │
│         │                         ▼                         │
│         │                  ┌──────────────┐                │
│         └─────────────────▶│    Cache     │                │
│                            │  IQueueMsg   │                │
│                            └──────┬───────┘                │
│                                   │                         │
│                                   ▼                         │
│                            ┌──────────────┐                │
│   ┌──────────────┐         │  QueueHost   │                │
│   │ScheduledWorker│───────▶│   (Poll)     │                │
│   └──────────────┘         └──────┬───────┘                │
│                                   │                         │
│                                   ▼                         │
│                            ┌──────────────┐                │
│                            │  Dispatcher  │                │
│                            │ (Worker Pool)│                │
│                            └──────┬───────┘                │
│                                   │                         │
│                    ┌──────────────┼──────────────┐         │
│                    ▼              ▼              ▼         │
│              ┌──────────┐   ┌──────────┐   ┌──────────┐  │
│              │ QueueJob │   │ QueueJob │   │ QueueJob │  │
│              └────┬─────┘   └────┬─────┘   └────┬─────┘  │
│                   │              │              │         │
│                   ▼              ▼              ▼         │
│              ┌──────────┐   ┌──────────┐   ┌──────────┐  │
│              │  Action  │   │  Action  │   │  Action  │  │
│              │(Process) │   │(Process) │   │(Process) │  │
│              └──────────┘   └──────────┘   └──────────┘  │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

## File Structure

```
Connected.Model/Collections/Queues/
  ├── IQueueAction.cs          # Action handler interface
  ├── IQueueContext.cs         # Context enqueue interface
  ├── IQueueMessage.cs         # Message entity interface
  └── IQueueMessageCache.cs    # Cache query interface

Connected.Extensions/Collections/Queues/
  ├── QueueAction.cs           # Action base class with ping support
  ├── QueueContext.cs          # Context base class with debouncing
  ├── QueueHost.cs             # Host base class with polling logic
  ├── QueueJob.cs              # Job processor with lifecycle management
  ├── QueueDispatcher.cs       # Dispatcher with priority tracking
  ├── QueueMessage.cs          # Message entity base record
  ├── QueueMessageCache.cs     # Cache implementation base
  └── Serialization/
      ├── DtoSerializer.cs     # DTO → binary serialization
      └── TypeSerializer.cs    # Type → string serialization
```

## Related Documentation

- [Caching Architecture](../../Caching/README.md)
- [Storage Layer](../../Storage/README.md)
- [Dependency Injection](../../DependencyInjection/README.md)
- [Dispatcher Pattern](../Concurrent/README.md)
- [Worker Services](../../Workers/README.md)
