using Microsoft.EntityFrameworkCore;
using Notifications.Api.Services;
using Notifications.Api.Workers;
using Notifications.Infrastructure.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient(
    "TestReceiver",
    client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["TestReceiver:BaseUrl"]!);
        client.Timeout = TimeSpan.FromSeconds(5);
    }
);

builder.Services.AddHostedService<OutboxWorker>();
builder.Services.AddControllers();

//add dbcontext
builder.Services.AddDbContext<NotificationsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<NotificationRetryService>();
var app = builder.Build();
app.UseHttpsRedirection();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTheme(ScalarTheme.Moon);
    });
}
app.MapControllers();

app.Run();
