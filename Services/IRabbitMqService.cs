using BackgroundServices.Models;

namespace BackgroundServices.Services;

public interface IRabbitMqService
{
    Task PublishAsync(EmailMessage msg);
    void Consume(Func<EmailMessage, Task> handler);
}