using MediatR;

namespace Api.Features.WatchAuction;

public static class WatchAuctionEndpoint
{
    public static void MapWatchAuctionEndpoint(this WebApplication app)
    {
        app.MapPost("/auctions/{id}/watch", async (Guid id, WatchAuctionRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new WatchAuctionCommand(id, request.UserId));
            return Results.Ok(result);
        });
    }
}

public record WatchAuctionRequest(Guid UserId);