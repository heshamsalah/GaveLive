namespace Api.Features.PlaceBid;

public record BidPlaced(Guid AuctionId, string BidderId, decimal Amount, DateTime PlacedAt);