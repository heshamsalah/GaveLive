using MediatR;

namespace Api.Features.WatchAuction;

public record WatchAuctionCommand(Guid AuctionId, Guid UserId) : IRequest<WatchAuctionResult>;

public record WatchAuctionResult(bool Success);