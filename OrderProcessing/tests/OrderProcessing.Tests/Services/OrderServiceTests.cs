using Microsoft.Extensions.Logging;
using Moq;
using OrderProcessing.Application.DTOs;
using OrderProcessing.Application.Interfaces.Repositories;
using OrderProcessing.Application.Services;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IRepository<Order>> _ordersRepo;
    private readonly Mock<IRepository<OutboxMessage>> _outboxRepo;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _ordersRepo = new Mock<IRepository<Order>>();
        _outboxRepo = new Mock<IRepository<OutboxMessage>>();
        _service = new OrderService(
            _ordersRepo.Object,
            _outboxRepo.Object,
            Mock.Of<ILogger<OrderService>>());
    }

    [Fact]
    public async Task CreateOrderAsync_AddsOrderAndOutboxMessage()
    {
        Order? addedOrder = null;
        OutboxMessage? addedOutbox = null;

        _ordersRepo.Setup(x => x.Add(It.IsAny<Order>()))
            .Callback<Order>(order => addedOrder = order);
        _ordersRepo.Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        _outboxRepo.Setup(x => x.Add(It.IsAny<OutboxMessage>()))
            .Callback<OutboxMessage>(message => addedOutbox = message);

        var request = new CreateOrderRequest
        {
            CustomerName = "Jane Doe",
            CustomerEmail = "jane@example.com",
            Amount = 99.99m
        };

        var order = await _service.CreateOrderAsync(request, "op-001");

        Assert.NotNull(addedOrder);
        Assert.NotNull(addedOutbox);
        Assert.Equal(order.Id, addedOrder!.Id);
        Assert.Equal("Jane Doe", addedOrder.CustomerName);
        Assert.Equal(OrderStatus.Created, addedOrder.Status);
        Assert.Equal("order.created", addedOutbox!.TopicName);
        Assert.Equal("op-001", addedOutbox.OperationId);
        Assert.Equal(ProcessingStatus.Pending, addedOutbox.Status);
        Assert.Contains(order.Id.ToString(), addedOutbox.BodyJson);

        _ordersRepo.Verify(x => x.Add(It.IsAny<Order>()), Times.Once);
        _outboxRepo.Verify(x => x.Add(It.IsAny<OutboxMessage>()), Times.Once);
        _ordersRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _outboxRepo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_ReturnsCreatedOrder()
    {
        _ordersRepo.Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var order = await _service.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerName = "Bob",
            CustomerEmail = "bob@example.com",
            Amount = 50m
        }, "op-002");

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal("Bob", order.CustomerName);
        Assert.Equal("bob@example.com", order.CustomerEmail);
        Assert.Equal(50m, order.Amount);
        Assert.Equal(OrderStatus.Created, order.Status);
    }

    [Fact]
    public async Task GetAllOrdersAsync_ReturnsOrdersOrderedByCreatedOnUtcDescending()
    {
        var older = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = "A",
            CustomerEmail = "a@a.com",
            Amount = 10m,
            Status = OrderStatus.Created,
            CreatedOnUtc = DateTime.UtcNow.AddMinutes(-10),
            UpdatedOnUtc = DateTime.UtcNow.AddMinutes(-10)
        };
        var newer = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = "B",
            CustomerEmail = "b@b.com",
            Amount = 20m,
            Status = OrderStatus.Created,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow
        };

        _ordersRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Order> { older, newer });

        var orders = await _service.GetAllOrdersAsync();

        Assert.Equal(2, orders.Count);
        Assert.Equal(newer.Id, orders[0].Id);
        Assert.Equal(older.Id, orders[1].Id);
    }
}
