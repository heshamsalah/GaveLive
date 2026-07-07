namespace Api.Features.EndAuction;

public record AuctionEnded(Guid AuctionId, string? WinnerId, decimal FinalPrice);