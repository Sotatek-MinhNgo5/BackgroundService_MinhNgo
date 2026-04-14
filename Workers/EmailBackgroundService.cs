using BackgroundServices.Channels;
using BackgroundServices.Models;
using BackgroundServices.Services;

namespace BackgroundServices.Workers;

public class EmailBackgroundService : BackgroundService
{
    private readonly EmailChannel _emailChannel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailBackgroundService> _logger;

    public EmailBackgroundService(
        EmailChannel emailChannel,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailBackgroundService> logger)
    {
        _emailChannel = emailChannel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailBackgroundService STARTED");

        await foreach (var message in _emailChannel.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessWithRetryAsync(message);

            await Task.Delay(500, stoppingToken);
        }
    }

    private async Task ProcessWithRetryAsync(EmailMessage message)
    {
        const int maxRetry = 3;

        for (int attempt = 1; attempt <= maxRetry; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                _logger.LogInformation(
                        "Processing {Email} (attempt {Attempt})",
                        message.To, attempt);

                // giả lập task nặng
                //await Task.Delay(2000);

                await emailSender.SendAsync(message);

                _logger.LogInformation("Sent successfully to {Email}", message.To);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Attempt {Attempt} failed: {Error}", attempt, ex.Message);

                if (attempt < maxRetry)
                    await Task.Delay(attempt * 1000);
                else
                    _logger.LogError("All retries exhausted for {Email}", message.To);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EmailBackgroundService STOPPING (graceful shutdown)");
        await base.StopAsync(cancellationToken);
    }
}
