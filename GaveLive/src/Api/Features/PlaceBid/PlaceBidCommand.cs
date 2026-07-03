using MediatR;

namespace Api.Features.PlaceBid;

public record PlaceBidCommand(
    Guid AuctionId,
    string BidderId,
    decimal Amount
) : IRequest<PlaceBidResult>;

public record PlaceBidResult(bool Success, string Message);