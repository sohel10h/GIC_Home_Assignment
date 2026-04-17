using OrderProcessing.Domain.Events;
using OrderProcessing.Domain.Interfaces;

namespace OrderProcessing.Api.BackgroundServices;

public class EventBusSubscriberService : IHostedService
{
    private readonly IInMemoryEventBus _eventBus;
    private readonly IServiceScopeFactory _scopeFactory;

    public EventBusSubscriberService(IInMemoryEventBus eventBus, IServiceScopeFactory scopeFactory)
    {
        _eventBus = eventBus;
        _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _eventBus.Subscribe<OrderCreatedEvent>("order.created", async (message, token) =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<OrderCreatedEvent>>();
            await handler.HandleAsync(message, token);
        });

        _eventBus.Subscribe<PaymentSucceededEvent>("payment.succeeded", async (message, token) =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<PaymentSucceededEvent>>();
            await handler.HandleAsync(message, token);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
