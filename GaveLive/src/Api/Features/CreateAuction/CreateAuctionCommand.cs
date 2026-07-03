using MediatR;

namespace Api.Features.CreateAuction;

public record CreateAuctionCommand(
    string Title,
    string Description,
    decimal StartingPrice,
    DateTime StartsAt,
    DateTime EndsAt
) : IRequest<CreateAuctionResult>;

public record CreateAuctionResult(bool Success, Guid AuctionId);