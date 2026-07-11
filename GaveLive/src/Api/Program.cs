using Api;
using Api.Features.CreateAuction;
using Api.Features.EndAuction;
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
    x.AddConsumer<WinnerNotificationConsumer>();
    x.AddConsumer<AuctionCardProjectionConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("messaging"));

        // AuctionCardProjectionConsumer gets its own explicit endpoint so we can
        // guarantee per-auction ordering: two events for the SAME auction (a bid,
        // or the auction ending) must never be processed out of order or in parallel,
        // otherwise the Redis card could end up reflecting stale data.
        cfg.ReceiveEndpoint("auction-card-projection", e =>
        {
            e.ConfigureConsumer<AuctionCardProjectionConsumer>(context, c =>
            {
                c.Message<BidPlaced>(m => m.UsePartitioner(16, p => p.Message.AuctionId));
                c.Message<AuctionEnded>(m => m.UsePartitioner(16, p => p.Message.AuctionId));
            });
        });

        // Everything else (OutbidNotificationConsumer, WinnerNotificationConsumer)
        // keeps using MassTransit's automatic convention-based endpoint setup.
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<AuctionEndingWorker>();
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