using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Api.Models;
using OrderProcessing.Application.DTOs;
using OrderProcessing.Application.Interfaces;

namespace OrderProcessing.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var operationId = Guid.NewGuid().ToString();
            var order = await _orderService.CreateOrderAsync(request, operationId);

            var response = new OrderResponse
            {
                Id = order.Id,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                Amount = order.Amount,
                Status = order.Status.ToString(),
                CreatedOnUtc = order.CreatedOnUtc
            };

            return CreatedAtAction(nameof(GetOrders), new { }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order");

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to create order.",
                detail = ex.Message
            });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        var response = orders.Select(order => new OrderResponse
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            Amount = order.Amount,
            Status = order.Status.ToString(),
            CreatedOnUtc = order.CreatedOnUtc
        }).ToList();

        return Ok(response);
    }
}
