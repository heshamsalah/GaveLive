using MediatR;

namespace Api.Features.WatchAuction;

public record GetWatcherCountQuery(Guid AuctionId) : IRequest<GetWatcherCountResult>;

public record GetWatcherCountResult(int Count);