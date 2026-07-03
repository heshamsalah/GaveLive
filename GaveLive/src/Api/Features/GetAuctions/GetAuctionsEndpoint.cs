using MediatR;

namespace Api.Features.GetAuctions;

public static class GetAuctionsEndpoint
{
    public static void MapGetAuctionsEndpoint(this WebApplication app)
    {
        app.MapGet("/auctions", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetAuctionsQuery());
            return Results.Ok(result.Auctions);
        });
    }
}