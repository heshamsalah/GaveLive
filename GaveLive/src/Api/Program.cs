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
using Microsoft.AspNetCore.Authentication.JwtBearer;

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
builder.Services.AddSignalR();

// Allows the standalone SignalR test page (or any browser client during
// development) to connect from a different origin than the API itself.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowTestClient", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddKeycloakJwtBearer(
        serviceName: "keycloak",
        realm: "gavelive",
        options =>
        {
            options.RequireHttpsMetadata = false;
            options.Audience = "account";

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var realmAccess = context.Principal?.FindFirst("realm_access")?.Value;
                    if (!string.IsNullOrEmpty(realmAccess))
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(realmAccess);
                        if (doc.RootElement.TryGetProperty("roles", out var roles))
                        {
                            var identity = context.Principal!.Identity as System.Security.Claims.ClaimsIdentity;
                            foreach (var role in roles.EnumerateArray())
                            {
                                identity!.AddClaim(new System.Security.Claims.Claim(
                                    System.Security.Claims.ClaimTypes.Role, role.GetString()!));
                            }
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });

builder.Services.AddAuthorization();

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

app.UseCors("AllowTestClient");

app.UseAuthentication();
app.UseAuthorization();

// Wire up all endpoints
app.MapCreateAuctionEndpoint();
app.MapGetAuctionsEndpoint();
app.MapPlaceBidEndpoint();
app.MapWatchAuctionEndpoint();
app.MapGetWatcherCountEndpoint();
app.MapHub<AuctionHub>("/hubs/auction");

app.Run();