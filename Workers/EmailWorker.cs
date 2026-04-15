using BackgroundServices.Services;
using BackgroundServices.Data;
using Microsoft.Extensions.DependencyInjection;

public class EmailWorker : BackgroundService
{
    private readonly IRabbitMqService _mq;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailWorker> _logger;

    public EmailWorker(IRabbitMqService mq, IServiceScopeFactory scopeFactory, ILogger<EmailWorker> logger)
    {
        _mq = mq;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _mq.Consume(async msg =>
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var log = db.EmailLogs.FirstOrDefault(x =>
                x.CampaignId == msg.CampaignId && x.Email == msg.To);

            if (log == null) return;

            try
            {
                await Task.Delay(100); // Simulate SES sending

                log.Status = "Sent";
                log.SentAt = DateTime.UtcNow;

                await db.SaveChangesAsync();

                _logger.LogInformation("Email sent successfully: {Email}", msg.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email: {Email}", msg.To);
            }
        });

        return Task.CompletedTask;
    }
}