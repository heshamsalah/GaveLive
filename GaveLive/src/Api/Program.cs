using Api;
using Api.Features.CreateAuction;
using Api.Features.GetAuctions;
using Api.Features.PlaceBid;
using Api.Features.WatchAuction;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Serilog must be set up before anything else touches logging
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});


// Add services
builder.AddNpgsqlDbContext<AuctionDbContext>(connectionName: "gavellive");
builder.AddRedisClient(connectionName: "cache");
builder.Services.AddMediatR(typeof(Program).Assembly);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OutbidNotificationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("messaging"));
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(); // structured log per HTTP request, free

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Wire up all endpoints
app.MapCreateAuctionEndpoint();
app.MapGetAuctionsEndpoint();
app.MapPlaceBidEndpoint();
app.MapWatchAuctionEndpoint();
app.MapGetWatcherCountEndpoint();

app.Run();