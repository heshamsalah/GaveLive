using Api.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.EndAuction;

public class AuctionEndingWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AuctionEndingWorker> _logger;

    public AuctionEndingWorker(IServiceProvider services, ILogger<AuctionEndingWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckForEndedAuctions(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task CheckForEndedAuctions(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var expiredAuctions = await db.Auctions
            .Where(a => a.Status == "Active" && a.EndsAt <= DateTime.UtcNow)
            .ToListAsync(stoppingToken);

        foreach (var auction in expiredAuctions)
        {
            var winningBid = await db.Bids
                .Where(b => b.AuctionId == auction.Id)
                .OrderByDescending(b => b.Amount)
                .FirstOrDefaultAsync(stoppingToken);

            auction.Status = "Ended";
            await db.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Auction {AuctionId} ended. Winner: {WinnerId}, Final price: {FinalPrice}",
                auction.Id,
                winningBid?.BidderId ?? "none",
                auction.CurrentPrice
            );

            await publishEndpoint.Publish(new AuctionEnded(
                auction.Id,
                winningBid?.BidderId,
                auction.CurrentPrice
            ), stoppingToken);
        }
    }
}