using Microsoft.Extensions.Logging;
using OrderProcessing.Application.Interfaces;
using OrderProcessing.Domain.Events;
using OrderProcessing.Domain.Interfaces;

namespace OrderProcessing.Application.EventHandlers;

public class OrderCreatedEventHandler : IIntegrationEventHandler<OrderCreatedEvent>
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(
        IPaymentService paymentService,
        ILogger<OrderCreatedEventHandler> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCreatedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling order created event for order {OrderId}. OperationId: {OperationId}",
            message.OrderId,
            message.OperationId);

        await _paymentService.ProcessPaymentAsync(message.OrderId, message.Amount, message.OperationId);
    }
}
