using BackgroundServices.Models;

namespace BackgroundServices.Services;

public class FakeEmailSender : IEmailSender
{
    public async Task SendAsync(EmailMessage message)
    {
        await Task.Delay(1000);

        Console.WriteLine($"[FAKE EMAIL] Sent to: {message.To}");
    }
}