using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using OrderProcessing.Application.Interfaces;
using OrderProcessing.Application.Settings;
using OrderProcessing.Domain.Events;
using OrderProcessing.Domain.Interfaces;

namespace OrderProcessing.Api.BackgroundServices;

public class EventBusSubscriberService : IHostedService
{
    private readonly IInMemoryEventBus _eventBus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RetrySettings _retrySettings;
    private readonly ILogger<EventBusSubscriberService> _logger;

    public EventBusSubscriberService(
        IInMemoryEventBus eventBus,
        IServiceScopeFactory scopeFactory,
        IOptions<RetrySettings> retryOptions,
        ILogger<EventBusSubscriberService> logger)
    {
        _eventBus = eventBus;
        _scopeFactory = scopeFactory;
        _retrySettings = retryOptions.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _eventBus.Subscribe<OrderCreatedEvent>("order.created", async (message, token) =>
        {
            var pipeline = BuildRetryPipeline("OrderCreatedEvent", message.OperationId);
            await pipeline.ExecuteAsync(async innerToken =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<OrderCreatedEvent>>();
                await handler.HandleAsync(message, innerToken);
            }, token);
        });

        _eventBus.Subscribe<PaymentSucceededEvent>("payment.succeeded", async (message, token) =>
        {
            var pipeline = BuildRetryPipeline("PaymentSucceededEvent", message.OperationId);
            await pipeline.ExecuteAsync(async innerToken =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<PaymentSucceededEvent>>();
                await handler.HandleAsync(message, innerToken);
            }, token);
        });

        _eventBus.Subscribe<OrderNotificationEvent>("order.notification", async (message, token) =>
        {
            var pipeline = BuildRetryPipeline("OrderNotificationEvent", message.OperationId);
            await pipeline.ExecuteAsync(async innerToken =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<OrderNotificationEvent>>();
                await handler.HandleAsync(message, innerToken);
            }, token);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private ResiliencePipeline BuildRetryPipeline(string eventName, string operationId)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _retrySettings.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(_retrySettings.RetryDelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = async args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Retry {Attempt} of {Max} for {EventName}/{OperationId}. Waiting {Delay}ms.",
                        args.AttemptNumber + 1,
                        _retrySettings.MaxRetryAttempts,
                        eventName,
                        operationId,
                        args.RetryDelay.TotalMilliseconds);

                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var incomingRequests = scope.ServiceProvider.GetRequiredService<IIncomingRequestService>();
                    await incomingRequests.IncrementRetryAsync(eventName, operationId);
                }
            })
            .Build();
    }
}
