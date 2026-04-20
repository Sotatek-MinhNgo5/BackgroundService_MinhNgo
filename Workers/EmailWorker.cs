using BackgroundServices.Data;
using BackgroundServices.Models;
using BackgroundServices.Services;
using Microsoft.EntityFrameworkCore;

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

            if (log == null)
            {
                _logger.LogWarning("EmailLog not found: CampaignId={CampaignId}, Email={Email}",
                    msg.CampaignId, msg.To);
                return;
            }

            try
            {
                await Task.Delay(10);

                log.Status = "Sent";
                log.SentAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

                _logger.LogInformation("Sent: {Email} (Campaign {CampaignId})", msg.To, msg.CampaignId);

                await TryMarkCampaignDoneAsync(db, msg.CampaignId);
            }
            catch (Exception ex)
            {
                log.RetryCount++;

                if (log.RetryCount >= 3)
                {
                    log.Status = "Failed";
                    _logger.LogError(ex, "FAILED after 3 retries: {Email}", msg.To);
                }
                else
                {
                    log.Status = "Pending";
                    await _mq.PublishAsync(new EmailMessage
                    {
                        To = msg.To,
                        CampaignId = msg.CampaignId
                    });
                    _logger.LogWarning("Retry {Count} for {Email}", log.RetryCount, msg.To);
                }

                await db.SaveChangesAsync();
            }
        });

        return Task.CompletedTask;
    }

    private static async Task TryMarkCampaignDoneAsync(AppDb db, int campaignId)
    {
        var hasPending = db.EmailLogs.Any(x =>
            x.CampaignId == campaignId && x.Status == "Pending");

        if (!hasPending)
        {
            var campaign = await db.Campaigns.FindAsync(campaignId);
            if (campaign != null && campaign.Status == "Running")
            {
                campaign.Status = "Done";
                await db.SaveChangesAsync();
            }
        }
    }
}