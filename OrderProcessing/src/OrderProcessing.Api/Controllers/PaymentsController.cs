using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Api.Models;
using OrderProcessing.Application.Interfaces;

namespace OrderProcessing.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<PaymentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayments()
    {
        var payments = await _paymentService.GetAllPaymentsAsync();
        var response = payments.Select(payment => new PaymentResponse
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            Status = payment.Status.ToString(),
            CreatedOnUtc = payment.CreatedOnUtc
        }).ToList();

        return Ok(response);
    }
}
