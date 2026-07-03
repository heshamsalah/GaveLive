using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.GetAuctions;

public class GetAuctionsHandler : IRequestHandler<GetAuctionsQuery, GetAuctionsResult>
{
    private readonly AuctionDbContext _db;

    public GetAuctionsHandler(AuctionDbContext db)
    {
        _db = db;
    }

    public async Task<GetAuctionsResult> Handle(GetAuctionsQuery query, CancellationToken cancellationToken)
    {
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

        return new GetAuctionsResult(auctions);
    }
}