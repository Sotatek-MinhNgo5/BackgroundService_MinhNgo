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
            Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),

            DispatchConsumersAsync = true,

            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        var conn = factory.CreateConnection();
        _channel = conn.CreateModel();

        _channel.QueueDeclare(
            queue: "email_queue",
            durable: true,       
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public Task PublishAsync(EmailMessage msg)
    {
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));

        var props = _channel.CreateBasicProperties();
        props.Persistent = true; 

        _channel.BasicPublish("", "email_queue", props, body);

        return Task.CompletedTask;
    }

    public void Consume(Func<EmailMessage, Task> handler)
    {
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 100, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (ch, ea) =>
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<EmailMessage>(
                    Encoding.UTF8.GetString(ea.Body.ToArray()));

                await handler(msg!);

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch
            {
                _channel.BasicNack(ea.DeliveryTag, false, requeue: true);
            }
        };

        _channel.BasicConsume("email_queue", autoAck: false, consumer);
    }
}