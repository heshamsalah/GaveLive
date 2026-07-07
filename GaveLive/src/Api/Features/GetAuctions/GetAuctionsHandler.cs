using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Api.Features.GetAuctions;

public class GetAuctionsHandler : IRequestHandler<GetAuctionsQuery, GetAuctionsResult>
{
    private readonly AuctionDbContext _db;
    private readonly IConnectionMultiplexer _redis;

    public GetAuctionsHandler(AuctionDbContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redis = redis;
    }

    public async Task<GetAuctionsResult> Handle(GetAuctionsQuery query, CancellationToken cancellationToken)
    {
        var cache = _redis.GetDatabase();

        var ids = await _db.Auctions
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var results = new List<AuctionDto>();

        foreach (var id in ids)
        {
            var key = $"auction-card:{id}";
            var cached = await cache.StringGetAsync(key);

            if (cached.HasValue)
            {
                var card = JsonSerializer.Deserialize<AuctionCard>(cached!)!;
                results.Add(new AuctionDto(
                    card.AuctionId,
                    card.Title,
                    card.Description,
                    card.CurrentPrice,
                    card.EndsAt,
                    card.Status));
            }
            else
            {
                var auction = await _db.Auctions.FindAsync([id], cancellationToken);
                if (auction is null) continue;

                results.Add(new AuctionDto(
                    auction.Id,
                    auction.Title,
                    auction.Description,
                    auction.CurrentPrice,
                    auction.EndsAt,
                    auction.Status));
            }
        }

        return new GetAuctionsResult(results);
    }
}