using BackgroundServices.Models;

namespace BackgroundServices.Services;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message);
}

