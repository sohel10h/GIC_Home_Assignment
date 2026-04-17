using Microsoft.Extensions.Logging;
using Moq;
using OrderProcessing.Application.EventHandlers;
using OrderProcessing.Application.Interfaces;
using OrderProcessing.Domain.Events;

namespace OrderProcessing.Tests.EventHandlers;

public class PaymentSucceededEventHandlerTests
{
    private readonly Mock<INotificationService> _notificationService;
    private readonly Mock<IIncomingRequestService> _incomingService;
    private readonly PaymentSucceededEventHandler _handler;

    public PaymentSucceededEventHandlerTests()
    {
        _notificationService = new Mock<INotificationService>();
        _incomingService = new Mock<IIncomingRequestService>();
        _handler = new PaymentSucceededEventHandler(
            _notificationService.Object,
            _incomingService.Object,
            Mock.Of<ILogger<PaymentSucceededEventHandler>>());
    }

    [Fact]
    public async Task HandleAsync_SendsNotificationAndMarksRequestCompleted()
    {
        _incomingService.Setup(x => x.HasProcessedAsync("PaymentSucceededEvent", "op-002"))
            .ReturnsAsync(false);

        var @event = new PaymentSucceededEvent
        {
            OperationId = "op-002",
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            Amount = 49.99m
        };

        await _handler.HandleAsync(@event);

        _incomingService.Verify(x => x.StartProcessingAsync("PaymentSucceededEvent", "op-002"), Times.Once);
        _notificationService.Verify(x => x.SendNotificationAsync(@event.OrderId, @event.Amount, @event.OperationId), Times.Once);
        _incomingService.Verify(x => x.MarkCompletedAsync("PaymentSucceededEvent", "op-002"), Times.Once);
        _incomingService.Verify(x => x.MarkErrorAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SkipsDuplicateEvent()
    {
        _incomingService.Setup(x => x.HasProcessedAsync("PaymentSucceededEvent", "op-dup"))
            .ReturnsAsync(true);

        var @event = new PaymentSucceededEvent
        {
            OperationId = "op-dup",
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            Amount = 49.99m
        };

        await _handler.HandleAsync(@event);

        _incomingService.Verify(x => x.StartProcessingAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _notificationService.Verify(x => x.SendNotificationAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
        _incomingService.Verify(x => x.MarkCompletedAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
