using MediatR;

namespace Api.Features.GetAuctions;

public record GetAuctionsQuery() : IRequest<GetAuctionsResult>;

public record GetAuctionsResult(List<AuctionDto> Auctions);

public record AuctionDto(
    Guid Id,
    string Title,
    string Description,
    decimal CurrentPrice,
    DateTime EndsAt,
    string Status
);