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



