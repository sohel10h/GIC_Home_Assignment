using Microsoft.Extensions.Logging;
using Moq;
using OrderProcessing.Application.EventHandlers;
using OrderProcessing.Application.Interfaces;
using OrderProcessing.Domain.Events;

namespace OrderProcessing.Tests.EventHandlers;

public class OrderCreatedEventHandlerTests
{
    private readonly Mock<IPaymentService> _paymentService;
    private readonly Mock<IIncomingRequestService> _incomingService;
    private readonly OrderCreatedEventHandler _handler;

    public OrderCreatedEventHandlerTests()
    {
        _paymentService = new Mock<IPaymentService>();
        _incomingService = new Mock<IIncomingRequestService>();
        _handler = new OrderCreatedEventHandler(
            _paymentService.Object,
            _incomingService.Object,
            Mock.Of<ILogger<OrderCreatedEventHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ProcessesPaymentAndMarksRequestCompleted()
    {
        _incomingService.Setup(x => x.HasProcessedAsync("OrderCreatedEvent", "op-001"))
            .ReturnsAsync(false);

        var @event = new OrderCreatedEvent
        {
            OperationId = "op-001",
            OrderId = Guid.NewGuid(),
            CustomerName = "Jane",
            CustomerEmail = "jane@example.com",
            Amount = 49.99m
        };

        await _handler.HandleAsync(@event);

        _incomingService.Verify(x => x.StartProcessingAsync("OrderCreatedEvent", "op-001"), Times.Once);
        _paymentService.Verify(x => x.ProcessPaymentAsync(@event.OrderId, @event.Amount, @event.OperationId), Times.Once);
        _incomingService.Verify(x => x.MarkCompletedAsync("OrderCreatedEvent", "op-001"), Times.Once);
        _incomingService.Verify(x => x.MarkErrorAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SkipsDuplicateEvent()
    {
        _incomingService.Setup(x => x.HasProcessedAsync("OrderCreatedEvent", "op-dup"))
            .ReturnsAsync(true);

        var @event = new OrderCreatedEvent
        {
            OperationId = "op-dup",
            OrderId = Guid.NewGuid(),
            CustomerName = "Jane",
            CustomerEmail = "jane@example.com",
            Amount = 49.99m
        };

        await _handler.HandleAsync(@event);

        _incomingService.Verify(x => x.StartProcessingAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _paymentService.Verify(x => x.ProcessPaymentAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
        _incomingService.Verify(x => x.MarkCompletedAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
