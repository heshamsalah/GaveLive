using MediatR;

namespace Api.Features.CreateAuction;

public static class CreateAuctionEndpoint
{
    public static void MapCreateAuctionEndpoint(this WebApplication app)
    {
        app.MapPost("/auctions", async (
            IMediator mediator,
            CreateAuctionCommand command) =>
        {
            var result = await mediator.Send(command);

            if (!result.Success)
                return Results.BadRequest("Failed to create auction");

            return Results.Created($"/auctions/{result.AuctionId}", result);
        });
    }
}