# Order Processing System

I went with an In-Memory Event Bus and EF Core InMemory DB, following the Outbox pattern and Clean Architecture principles. Since the In-Memory Event Bus doesn't work across multiple applications or servers, I consolidated the three microservices into a single application to simulate the event-driven architecture flow.
In the Outbox pattern, the publisher writes to an Outbox table, while the consumer tracks processed messages in an IncomingRequest table. The two can be matched using an Operation ID, though the exact matching strategy may vary depending on business requirements.

# Run Locally
```
dotnet restore
dotnet run --project src/OrderProcessing.Api
```
Once running, open your browser and go to:

**Swagger UI:** `http://localhost:9489/swagger`

```
OrderProcessing.Api           → HTTP layer, background workers, middleware
OrderProcessing.Application   → Business logic, services, event handlers
OrderProcessing.Domain        → Entities, events, enums, interfaces
OrderProcessing.Infrastructure → EF Core (in-memory), repositories, event bus


# Event Flow

POST /api/orders
       │
       ▼
  1. Order created → saved to DB
       
  2. OutboxMessage written (topic: OrderCreated)
       
  3. OrderCreatedOutboxWorker picks it up 
      
  4. Payment processed → saved to DB
      
  5.  OutboxMessage written (topic: PaymentSucceeded)
      
  6. PaymentSucceededOutboxWorker picks it up
       
  7. Notification created → saved to DB
       
  8. OutboxMessage written (topic: OrderNotification)
       
  9. OrderNotificationOutboxWorker picks it up → marks complete
```




## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/orders` | Create a new order |
| GET | `/api/orders` | List all orders |
| GET | `/api/payments` | List all payments |
| GET | `/api/notifications` | List all notifications |

## Known Limitations

- I added duplicate protection on the consumer side, but I have not fully finished that part yet. Because of time limitation, I did not complete the logic to handle idempotency-related cases separately from other failures.
- I know one way to improve the outbox polling is to use `UPDLOCK, READPAST` in SQL Server so one instance can skip rows already being handled by another instance. I did not implement that here because EF Core ORM does not support it directly in the normal LINQ query flow, so I would need to write a raw SQL query in C# for that part.
- Saga pattern is also not implemented. I know this would be needed for a more complete distributed workflow, but I did not finish that part due to time limitation.
