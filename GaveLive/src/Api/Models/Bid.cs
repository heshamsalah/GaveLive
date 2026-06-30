namespace Api.Models;

public class Bid
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public string BidderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PlacedAt { get; set; }

    // Link to Auction
    public Auction Auction { get; set; } = null!;
}