using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Api.Features.GetAuctions;

namespace Api.Features.PlaceBid;

public class AuctionCardProjectionConsumer : IConsumer<BidPlaced>
{
    private readonly ILogger<AuctionCardProjectionConsumer> _logger;
    private readonly AuctionDbContext _db;
    private readonly IConnectionMultiplexer _redis;

    public AuctionCardProjectionConsumer(
        ILogger<AuctionCardProjectionConsumer> logger,
        AuctionDbContext db,
        IConnectionMultiplexer redis)
    {
        _logger = logger;
        _db = db;
        _redis = redis;
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
    }
}