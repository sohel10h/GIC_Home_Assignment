namespace OrderProcessing.Application.Settings;

public class OutboxSettings
{
    public int PendingBatchSize { get; set; } = 20;
}
