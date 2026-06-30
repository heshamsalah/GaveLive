using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api;

public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options)
        : base(options) { }

    public DbSet<Auction> Auctions { get; set; }
    public DbSet<Bid> Bids { get; set; }
}

public class AuctionDbContextFactory : IDesignTimeDbContextFactory<AuctionDbContext>
{
    public AuctionDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuctionDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=gavellive;Username=postgres;Password=password");
        return new AuctionDbContext(optionsBuilder.Options);
    }
}