using Microsoft.Extensions.Logging;
using Moq;
using OrderProcessing.Application.Interfaces.Repositories;
using OrderProcessing.Application.Services;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Tests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IRepository<Payment>> _paymentsRepo;
    private readonly Mock<IRepository<OutboxMessage>> _outboxRepo;
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _paymentsRepo = new Mock<IRepository<Payment>>();
        _outboxRepo = new Mock<IRepository<OutboxMessage>>();
        _service = new PaymentService(
            _paymentsRepo.Object,
            _outboxRepo.Object,
            Mock.Of<ILogger<PaymentService>>());
    }

    [Fact]
    public async Task ProcessPaymentAsync_AddsPaymentAndOutboxMessage()
    {
        Payment? addedPayment = null;
        OutboxMessage? addedOutbox = null;

        _paymentsRepo.Setup(x => x.Add(It.IsAny<Payment>()))
            .Callback<Payment>(payment => addedPayment = payment);
        _paymentsRepo.Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        _outboxRepo.Setup(x => x.Add(It.IsAny<OutboxMessage>()))
            .Callback<OutboxMessage>(message => addedOutbox = message);

        var orderId = Guid.NewGuid();

        await _service.ProcessPaymentAsync(orderId, 75.00m, "op-pay-001");

        Assert.NotNull(addedPayment);
        Assert.NotNull(addedOutbox);
        Assert.Equal(orderId, addedPayment!.OrderId);
        Assert.Equal(75.00m, addedPayment.Amount);
        Assert.Equal(ProcessingStatus.Completed, addedPayment.Status);
        Assert.Equal("payment.succeeded", addedOutbox!.TopicName);
        Assert.Equal("op-pay-001", addedOutbox.OperationId);
        Assert.Equal(ProcessingStatus.Pending, addedOutbox.Status);
        Assert.Contains(orderId.ToString(), addedOutbox.BodyJson);

        _paymentsRepo.Verify(x => x.Add(It.IsAny<Payment>()), Times.Once);
        _outboxRepo.Verify(x => x.Add(It.IsAny<OutboxMessage>()), Times.Once);
        _paymentsRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _outboxRepo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAllPaymentsAsync_ReturnsPaymentsOrderedByCreatedOnUtcDescending()
    {
        var older = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 10m,
            Status = ProcessingStatus.Completed,
            CreatedOnUtc = DateTime.UtcNow.AddMinutes(-10),
            UpdatedOnUtc = DateTime.UtcNow.AddMinutes(-10)
        };
        var newer = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 20m,
            Status = ProcessingStatus.Completed,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow
        };

        _paymentsRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Payment> { older, newer });

        var payments = await _service.GetAllPaymentsAsync();

        Assert.Equal(2, payments.Count);
        Assert.Equal(newer.Id, payments[0].Id);
        Assert.Equal(older.Id, payments[1].Id);
    }
}
