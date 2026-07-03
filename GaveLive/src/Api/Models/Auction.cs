using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public class Auction
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Status { get; set; } = "Active";

    [Timestamp]
    public uint RowVersion { get; set; }
}