using BackgroundServices.Models;
using BackgroundServices.Services;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class RabbitMqService : IRabbitMqService
{
    private readonly IModel _channel;

    public RabbitMqService(IConfiguration config)
    {
        var factory = new ConnectionFactory()
        {
            HostName = config["RabbitMQ:HostName"], 
            UserName = config["RabbitMQ:UserName"],
            Password = config["RabbitMQ:Password"],
            Port = int.Parse(config["RabbitMQ:Port"] ?? "5672")
        };
        factory.AutomaticRecoveryEnabled = true;
        var conn = factory.CreateConnection();
        _channel = conn.CreateModel();

        _channel.QueueDeclare(queue: "email_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
    }

    public Task PublishAsync(EmailMessage msg)
    {
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));
        _channel.BasicPublish("", "email_queue", null, body);
        return Task.CompletedTask;
    }

    public void Consume(Func<EmailMessage, Task> handler)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            var msg = JsonConvert.DeserializeObject<EmailMessage>(
                Encoding.UTF8.GetString(ea.Body.ToArray()));

            await handler(msg);
        };

        _channel.BasicConsume("email_queue", true, consumer);
    }
}