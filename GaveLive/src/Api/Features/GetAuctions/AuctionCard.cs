namespace Api.Features.GetAuctions;

public record AuctionCard(
    Guid AuctionId,
    string Title,
    string Description,
    decimal CurrentPrice,
    int BidCount,
    DateTime EndsAt,
    string Status);