using Api.Models;
using MediatR;

namespace Api.Features.CreateAuction;

public class CreateAuctionHandler : IRequestHandler<CreateAuctionCommand, CreateAuctionResult>
{
    private readonly AuctionDbContext _db;

    public CreateAuctionHandler(AuctionDbContext db)
    {
        _db = db;
    }

    public async Task<CreateAuctionResult> Handle(CreateAuctionCommand command, CancellationToken cancellationToken)
    {
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Title = command.Title,
            Description = command.Description,
            StartingPrice = command.StartingPrice,
            CurrentPrice = command.StartingPrice,
            StartsAt = DateTime.SpecifyKind(command.StartsAt, DateTimeKind.Utc),
            EndsAt = DateTime.SpecifyKind(command.EndsAt, DateTimeKind.Utc),
            Status = "Active"
        };

        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateAuctionResult(true, auction.Id);
    }
}