using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Api.Features.GetAuctions;

public class GetAuctionsHandler : IRequestHandler<GetAuctionsQuery, GetAuctionsResult>
{
    private const string CacheKey = "auctions:all";

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

        // 1. Try the cache first
        var cached = await cache.StringGetAsync(CacheKey);
        if (cached.HasValue)
        {
            var cachedAuctions = JsonSerializer.Deserialize<List<AuctionDto>>(cached!)!;
            return new GetAuctionsResult(cachedAuctions);
        }

        // 2. Cache miss — go to Postgres
        var auctions = await _db.Auctions
            .Select(a => new AuctionDto(
                a.Id,
                a.Title,
                a.Description,
                a.CurrentPrice,
                a.EndsAt,
                a.Status
            ))
            .ToListAsync(cancellationToken);

        // 3. Store it in Redis for next time, expiring after 10 seconds
        var json = JsonSerializer.Serialize(auctions);
        await cache.StringSetAsync(CacheKey, json, TimeSpan.FromSeconds(10));

        return new GetAuctionsResult(auctions);
    }
}