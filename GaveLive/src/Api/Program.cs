using Api;
using Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<AuctionDbContext>(options =>
    options.UseNpgsql(builder.Configuration
        .GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CREATE AUCTION
app.MapPost("/auctions", async (AuctionDbContext db, Auction auction) =>
{
    auction.Id = Guid.NewGuid();
    auction.CurrentPrice = auction.StartingPrice;
    auction.StartsAt = DateTime.SpecifyKind(auction.StartsAt, DateTimeKind.Utc);
    auction.EndsAt = DateTime.SpecifyKind(auction.EndsAt, DateTimeKind.Utc);
    db.Auctions.Add(auction);
    await db.SaveChangesAsync();
    return Results.Created($"/auctions/{auction.Id}", auction);
});

// GET ALL AUCTIONS
app.MapGet("/auctions", async (AuctionDbContext db) =>
{
    var auctions = await db.Auctions.ToListAsync();
    return Results.Ok(auctions);
});

// PLACE A BID
app.MapPost("/auctions/{id}/bids", async (AuctionDbContext db, Guid id, Bid bid) =>
{
    var auction = await db.Auctions.FindAsync(id);
    if (auction == null) return Results.NotFound();
    if (bid.Amount <= auction.CurrentPrice)
        return Results.BadRequest("Bid must be higher than current price!");

    bid.Id = Guid.NewGuid();
    bid.AuctionId = id;
    bid.PlacedAt = DateTime.UtcNow;
    auction.CurrentPrice = bid.Amount;

    db.Bids.Add(bid);
    await db.SaveChangesAsync();
    return Results.Created($"/auctions/{id}/bids/{bid.Id}", bid);
});
app.Run();