using MediatR;
using StackExchange.Redis;

namespace Api.Features.WatchAuction;

public class WatchAuctionHandler : IRequestHandler<WatchAuctionCommand, WatchAuctionResult>
{
    private readonly IConnectionMultiplexer _redis;

    public WatchAuctionHandler(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<WatchAuctionResult> Handle(WatchAuctionCommand command, CancellationToken cancellationToken)
    {
        var cache = _redis.GetDatabase();
        var key = $"watching:{command.AuctionId}:{command.UserId}";

        await cache.StringSetAsync(key, "1", TimeSpan.FromSeconds(30));

        return new WatchAuctionResult(true);
    }
}