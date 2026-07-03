using MediatR;

namespace Api.Features.PlaceBid;

public static class PlaceBidEndpoint
{
    public static void MapPlaceBidEndpoint(this WebApplication app)
    {
        app.MapPost("/auctions/{id}/bids", async (
            IMediator mediator,
            Guid id,
            PlaceBidRequest request) =>
        {
            var command = new PlaceBidCommand(id, request.BidderId, request.Amount);
            var result = await mediator.Send(command);

            if (!result.Success)
                return Results.BadRequest(result.Message);

            return Results.Ok(result);
        });
    }
}

public record PlaceBidRequest(string BidderId, decimal Amount);