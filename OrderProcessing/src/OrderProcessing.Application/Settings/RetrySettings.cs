namespace OrderProcessing.Application.Settings;

public class RetrySettings
{
    public int MaxRetryAttempts { get; set; } = 5;
    public int RetryDelaySeconds { get; set; } = 2;
}
