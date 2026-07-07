using MassTransit;
using Microsoft.EntityFrameworkCore;
using Api;

namespace Api.Features.EndAuction;

public class WinnerNotificationConsumer : IConsumer<AuctionEnded>
{
    private readonly ILogger<WinnerNotificationConsumer> _logger;
    private readonly AuctionDbContext _db;

    public WinnerNotificationConsumer(ILogger<WinnerNotificationConsumer> logger, AuctionDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task Consume(ConsumeContext<AuctionEnded> context)
    {
        var msg = context.Message;

        if (string.IsNullOrEmpty(msg.WinnerId))
        {
            _logger.LogInformation(
                "Auction {AuctionId} ended with no bids — nothing to settle",
                msg.AuctionId);
            return;
        }

        // Winner notification — same "log it for now" stub as OutbidNotificationConsumer
        _logger.LogInformation(
            "Notification: Bidder {WinnerId} won auction {AuctionId} at {FinalPrice:C}",
            msg.WinnerId, msg.AuctionId, msg.FinalPrice);

        // Simulated payment step — stub only, no real payment provider yet
        var success = await SimulatePaymentAsync(msg.WinnerId, msg.FinalPrice);

        _logger.LogInformation(
            "Payment {Result} for auction {AuctionId}, bidder {WinnerId}, amount {Amount:C}",
            success ? "SUCCEEDED" : "FAILED", msg.AuctionId, msg.WinnerId, msg.FinalPrice);

        // Optional: persist payment status on the auction so it shows up in "My bids"/"My listings" later
        var auction = await _db.Auctions.FindAsync(msg.AuctionId);
        if (auction is not null)
        {
            auction.PaymentStatus = success ? "Paid" : "Failed";
            await _db.SaveChangesAsync();
        }
    }

    private static Task<bool> SimulatePaymentAsync(string winnerId, decimal amount)
    {
        // Deliberately fake — this is a stub, not a payment integration.
        // Swap for a real provider only when brief §8 splits Payments into its own service.
        return Task.FromResult(true);
    }
}