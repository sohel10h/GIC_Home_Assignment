using Microsoft.Extensions.Logging;
using OrderProcessing.Application.Interfaces;
using OrderProcessing.Application.Interfaces.Repositories;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;
using OrderProcessing.Domain.Events;
using OrderProcessing.Domain.Interfaces;

namespace OrderProcessing.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IRepository<Payment> _paymentsRepo;
    private readonly IInMemoryEventBus _eventBus;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IRepository<Payment> paymentsRepo,
        IInMemoryEventBus eventBus,
        ILogger<PaymentService> logger)
    {
        _paymentsRepo = paymentsRepo;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task ProcessPaymentAsync(Guid orderId, decimal amount, string operationId)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            Status = ProcessingStatus.Completed,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow
        };

        _paymentsRepo.Add(payment);
        await _paymentsRepo.SaveChangesAsync();

        await _eventBus.PublishAsync("payment.succeeded", new PaymentSucceededEvent
        {
            OperationId = operationId,
            OrderId = orderId,
            PaymentId = payment.Id,
            Amount = amount
        });

        _logger.LogInformation(
            "Payment {PaymentId} processed for order {OrderId}. OperationId: {OperationId}",
            payment.Id,
            orderId,
            operationId);
    }

    public async Task<List<Payment>> GetAllPaymentsAsync()
    {
        var payments = await _paymentsRepo.GetAllAsync();
        return payments.OrderByDescending(x => x.CreatedOnUtc).ToList();
    }
}
