using Api;
using Api.Features.CreateAuction;
using Api.Features.GetAuctions;
using Api.Features.PlaceBid;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<AuctionDbContext>(options =>
    options.UseNpgsql(builder.Configuration
        .GetConnectionString("DefaultConnection")));

builder.Services.AddMediatR(typeof(Program).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Wire up all endpoints
app.MapCreateAuctionEndpoint();
app.MapGetAuctionsEndpoint();
app.MapPlaceBidEndpoint();

app.Run();