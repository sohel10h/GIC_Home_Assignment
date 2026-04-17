using Microsoft.Extensions.Logging;
using OrderProcessing.Application.Interfaces;
using OrderProcessing.Domain.Events;
using OrderProcessing.Domain.Interfaces;

namespace OrderProcessing.Application.EventHandlers;

public class PaymentSucceededEventHandler : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentSucceededEventHandler> _logger;

    public PaymentSucceededEventHandler(
        INotificationService notificationService,
        ILogger<PaymentSucceededEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentSucceededEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling payment succeeded event for order {OrderId}. OperationId: {OperationId}",
            message.OrderId,
            message.OperationId);

        await _notificationService.SendNotificationAsync(message.OrderId, message.Amount, message.OperationId);
    }
}
