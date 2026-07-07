using Api;
using Api.Features.CreateAuction;
using Api.Features.GetAuctions;
using Api.Features.PlaceBid;
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

builder.Services.AddMediatR(typeof(Program).Assembly);

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

app.Run();