using Microsoft.EntityFrameworkCore;
using BackgroundServices.Data;
using BackgroundServices.Services;

var builder = WebApplication.CreateBuilder(args);

// DB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDb>(opt => opt.UseSqlServer(connectionString));

// RabbitMQ 
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

builder.Services.AddHostedService<EmailWorker>();

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

app.Run();