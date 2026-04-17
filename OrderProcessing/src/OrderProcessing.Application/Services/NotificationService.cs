using Microsoft.Extensions.Logging;
using OrderProcessing.Application.Interfaces;
using OrderProcessing.Application.Interfaces.Repositories;
using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _notificationsRepo;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IRepository<Notification> notificationsRepo,
        ILogger<NotificationService> logger)
    {
        _notificationsRepo = notificationsRepo;
        _logger = logger;
    }

    public async Task SendNotificationAsync(Guid orderId, decimal amount, string operationId)
    {
        var message = $"Payment of {amount:C} received for order {orderId}. Thank you!";

        _notificationsRepo.Add(new Notification
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Message = message,
            CreatedOnUtc = DateTime.UtcNow
        });

        await _notificationsRepo.SaveChangesAsync();

        _logger.LogInformation(
            "Notification created for order {OrderId}. OperationId: {OperationId}",
            orderId,
            operationId);
    }

    public async Task<List<Notification>> GetAllNotificationsAsync()
    {
        var notifications = await _notificationsRepo.GetAllAsync();
        return notifications.OrderByDescending(x => x.CreatedOnUtc).ToList();
    }
}
