using Microsoft.Extensions.Logging;
using OrderProcessing.Application.DTOs;
using OrderProcessing.Application.Interfaces;
using OrderProcessing.Application.Interfaces.Repositories;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Application.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _ordersRepo;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IRepository<Order> ordersRepo, ILogger<OrderService> logger)
    {
        _ordersRepo = ordersRepo;
        _logger = logger;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request, string operationId)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            Amount = request.Amount,
            Status = OrderStatus.Created,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow
        };

        _ordersRepo.Add(order);
        await _ordersRepo.SaveChangesAsync();

        _logger.LogInformation(
            "Order {OrderId} created for customer {CustomerName}. OperationId: {OperationId}",
            order.Id,
            order.CustomerName,
            operationId);

        return order;
    }

    public async Task<List<Order>> GetAllOrdersAsync()
    {
        var orders = await _ordersRepo.GetAllAsync();
        return orders.OrderByDescending(x => x.CreatedOnUtc).ToList();
    }
}
