using MediatR;

namespace Api.Features.WatchAuction;

public static class GetWatcherCountEndpoint
{
    public static void MapGetWatcherCountEndpoint(this WebApplication app)
    {
        app.MapGet("/auctions/{id}/watchers", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetWatcherCountQuery(id));
            return Results.Ok(result);
        });
    }
}