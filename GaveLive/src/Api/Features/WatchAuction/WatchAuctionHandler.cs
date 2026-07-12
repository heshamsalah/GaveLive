using MediatR;
using StackExchange.Redis;
using Microsoft.AspNetCore.SignalR;
using Api.Features.PlaceBid;

namespace Api.Features.WatchAuction;

public class WatchAuctionHandler : IRequestHandler<WatchAuctionCommand, WatchAuctionResult>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<AuctionHub> _hub;

    public WatchAuctionHandler(IConnectionMultiplexer redis, IHubContext<AuctionHub> hub)
    {
        _redis = redis;
        _hub = hub;
    }

    public async Task<WatchAuctionResult> Handle(WatchAuctionCommand command, CancellationToken cancellationToken)
    {
        var cache = _redis.GetDatabase();
        var key = $"watching:{command.AuctionId}:{command.UserId}";

        await cache.StringSetAsync(key, "1", TimeSpan.FromSeconds(30));

        // Count how many are currently watching this auction, then broadcast it
        var server = cache.Multiplexer.GetServer(cache.Multiplexer.GetEndPoints().First());
        var pattern = $"watching:{command.AuctionId}:*";
        var watcherCount = server.Keys(pattern: pattern).Count();

        await _hub.Clients.Group(command.AuctionId.ToString())
            .SendAsync("WatcherCountUpdated", new
            {
                auctionId = command.AuctionId,
                watcherCount
            }, cancellationToken);

        return new WatchAuctionResult(true);
    }
}