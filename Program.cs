using Microsoft.EntityFrameworkCore;
using BackgroundServices.Data;
using BackgroundServices.Models;
using BackgroundServices.Services;


var builder = WebApplication.CreateBuilder(args);

// DB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDb>(opt => opt.UseSqlServer(connectionString));

// RabbitMQ
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

// Worker
//builder.Services.AddHostedService<EmailWorker>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    await Task.Delay(3000);           
    db.Database.EnsureCreated();     
    Console.WriteLine("Database ready!");
}

//app.MapPost("/campaign", async (CampaignRequest req, IRabbitMqService mq, AppDb db) =>
//{
//    foreach (var email in req.Emails)
//    {
//        var log = new EmailLog
//        {
//            CampaignId = req.CampaignId,
//            Email = email,
//            Status = "Pending"
//        };

//        db.EmailLogs.Add(log);

//        await mq.PublishAsync(new EmailMessage
//        {
//            To = email,
//            CampaignId = req.CampaignId
//        });
//    }

//    await db.SaveChangesAsync();

//    return Results.Ok("Campaign queued!");
//});

// Tracking open
//app.MapGet("/track/open", async (int id, AppDb db) =>
//{
//    var log = await db.EmailLogs.FindAsync(id);
//    if (log != null)
//    {
//        log.Status = "Opened";
//        await db.SaveChangesAsync();
//    }

//    return Results.Ok();
//});

app.Run();