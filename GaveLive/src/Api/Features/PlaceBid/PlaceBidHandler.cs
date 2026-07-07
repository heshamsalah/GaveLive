using Api.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Api.Features.PlaceBid;

public class PlaceBidHandler : IRequestHandler<PlaceBidCommand, PlaceBidResult>
{
    private const string AuctionsCacheKey = "auctions:all";

    private readonly AuctionDbContext _db;
    private readonly ILogger<PlaceBidHandler> _logger;
    private readonly IConnectionMultiplexer _redis;

    public PlaceBidHandler(AuctionDbContext db, ILogger<PlaceBidHandler> logger, IConnectionMultiplexer redis)
    {
        _db = db;
        _logger = logger;
        _redis = redis;
    }

    public async Task<PlaceBidResult> Handle(PlaceBidCommand command, CancellationToken cancellationToken)
    {
        var auction = await _db.Auctions.FindAsync(command.AuctionId);

        if (auction == null)
            return new PlaceBidResult(false, "Auction not found");

        if (command.Amount <= auction.CurrentPrice)
            return new PlaceBidResult(false, "Bid must be higher than current price!");

        var bid = new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = command.AuctionId,
            BidderId = command.BidderId,
            Amount = command.Amount,
            PlacedAt = DateTime.UtcNow
        };

        auction.CurrentPrice = command.Amount;
        _db.Bids.Add(bid);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new PlaceBidResult(false, "Someone else just placed a bid! Please try again.");
        }

        // Invalidate the cached auction list so the next read gets the fresh price
        var cache = _redis.GetDatabase();
        await cache.KeyDeleteAsync(AuctionsCacheKey);

        _logger.LogInformation(
            "Bid placed {AuctionId} {BidderId} {Amount} {Timestamp}",
            bid.AuctionId,
            bid.BidderId,
            bid.Amount,
            bid.PlacedAt
        );

        return new PlaceBidResult(true, "Bid placed successfully!");
    }
}