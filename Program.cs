using BackgroundServices;
using BackgroundServices.Channels;
using BackgroundServices.Models;
using BackgroundServices.Services;
using BackgroundServices.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<EmailChannel>();
builder.Services.AddScoped<IEmailSender, FakeEmailSender>();
builder.Services.AddHostedService<EmailBackgroundService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/order", async (OrderRequest order, EmailChannel channel) =>
{
    var message = new EmailMessage
    {
        To = order.CustomerEmail,
        Subject = "Order Confirmation",
        Body = $"Your order {order.OrderId} has been received!"
    };

    await channel.Writer.WriteAsync(message);

    return Results.Ok(new
    {
        message = "Order received! Email will be sent shortly.",
        orderId = order.OrderId
    });
});

app.Run();