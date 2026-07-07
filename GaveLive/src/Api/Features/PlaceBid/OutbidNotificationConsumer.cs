using MassTransit;

namespace Api.Features.PlaceBid;

public class OutbidNotificationConsumer : IConsumer<BidPlaced>
{
    private readonly ILogger<OutbidNotificationConsumer> _logger;

    public OutbidNotificationConsumer(ILogger<OutbidNotificationConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BidPlaced> context)
    {
        var bid = context.Message;

        _logger.LogInformation(
            "Outbid notification triggered for auction {AuctionId} — new highest bid {Amount} by {BidderId}",
            bid.AuctionId,
            bid.Amount,
            bid.BidderId
        );

        return Task.CompletedTask;
    }
}