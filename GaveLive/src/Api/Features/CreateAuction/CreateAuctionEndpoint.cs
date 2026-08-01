using MediatR;

namespace Api.Features.CreateAuction;

public static class CreateAuctionEndpoint
{
    public static void MapCreateAuctionEndpoint(this WebApplication app)
    {
        app.MapPost("/auctions", async (
            IMediator mediator,
            CreateAuctionCommand command,
            HttpContext httpContext) =>
        {
            var sellerId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? httpContext.User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(sellerId))
                return Results.Unauthorized();

            var commandWithSeller = command with { SellerId = sellerId };

            var result = await mediator.Send(commandWithSeller);

            if (!result.Success)
                return Results.BadRequest("Failed to create auction");

            return Results.Created($"/auctions/{result.AuctionId}", result);
        })
        .RequireAuthorization(policy => policy.RequireRole("Seller"));
    }
}