using MediatR;
using StackExchange.Redis;

namespace Api.Features.WatchAuction;

public class GetWatcherCountHandler : IRequestHandler<GetWatcherCountQuery, GetWatcherCountResult>
{
    private readonly IConnectionMultiplexer _redis;

    public GetWatcherCountHandler(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<GetWatcherCountResult> Handle(GetWatcherCountQuery query, CancellationToken cancellationToken)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var pattern = $"watching:{query.AuctionId}:*";

        var count = 0;
        await foreach (var _ in server.KeysAsync(pattern: pattern))
        {
            count++;
        }

        return new GetWatcherCountResult(count);
    }
}