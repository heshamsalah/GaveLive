using Api.Features.EndAuction;
using Api.Features.GetAuctions;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace Api.Features.PlaceBid;

public class AuctionCardProjectionConsumer :
    IConsumer<BidPlaced>,
    IConsumer<AuctionEnded>
{
    private readonly ILogger<AuctionCardProjectionConsumer> _logger;
    private readonly AuctionDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<AuctionHub> _hub;

    public AuctionCardProjectionConsumer(
        ILogger<AuctionCardProjectionConsumer> logger,
        AuctionDbContext db,
        IConnectionMultiplexer redis,
        IHubContext<AuctionHub> hub)
    {
        _logger = logger;
        _db = db;
        _redis = redis;
        _hub = hub;
    }

    public async Task Consume(ConsumeContext<BidPlaced> context)
    {
        var msg = context.Message;

        var auction = await _db.Auctions.FindAsync(msg.AuctionId);
        if (auction is null)
        {
            _logger.LogWarning("AuctionCardProjection: auction {AuctionId} not found", msg.AuctionId);
            return;
        }

        var bidCount = await _db.Bids.CountAsync(b => b.AuctionId == msg.AuctionId);

        var card = new AuctionCard(
            auction.Id,
            auction.Title,
            auction.Description,
            auction.CurrentPrice,
            bidCount,
            auction.EndsAt,
            auction.Status);

        var db = _redis.GetDatabase();
        var key = $"auction-card:{auction.Id}";
        await db.StringSetAsync(key, JsonSerializer.Serialize(card));

        _logger.LogInformation(
            "Projection updated for auction {AuctionId}: price={Price} bids={BidCount}",
            auction.Id, auction.CurrentPrice, bidCount);

        // NEW — broadcast to everyone watching this specific auction
        await _hub.Clients.Group(auction.Id.ToString())
            .SendAsync("PriceUpdated", new
            {
                auctionId = auction.Id,
                price = auction.CurrentPrice,
                bidCount
            });
    }

    public async Task Consume(ConsumeContext<AuctionEnded> context)
    {
        var msg = context.Message;

        var auction = await _db.Auctions.FindAsync(msg.AuctionId);
        if (auction is null)
        {
            _logger.LogWarning("AuctionCardProjection: auction {AuctionId} not found", msg.AuctionId);
            return;
        }

        var bidCount = await _db.Bids.CountAsync(b => b.AuctionId == msg.AuctionId);

        var card = new AuctionCard(
            auction.Id,
            auction.Title,
            auction.Description,
            auction.CurrentPrice,
            bidCount,
            auction.EndsAt,
            auction.Status);

        var db = _redis.GetDatabase();
        var key = $"auction-card:{auction.Id}";
        await db.StringSetAsync(key, JsonSerializer.Serialize(card));

        _logger.LogInformation(
            "Projection updated for ended auction {AuctionId}: status={Status}",
            auction.Id, auction.Status);

        // NEW — tell watchers this auction just ended
        await _hub.Clients.Group(auction.Id.ToString())
            .SendAsync("AuctionEnded", new
            {
                auctionId = auction.Id,
                status = auction.Status
            });
    }
}