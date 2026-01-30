using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProductCheckerBack.Models;

namespace ProductCheckerBack
{
    internal class ArtemisDbContext : BaseDbContext
    {
        public DbSet<ListingStatus> ListingStatus { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL(Configuration.GetArtemisConnectionString(),
                options => options.EnableRetryOnFailure(5, TimeSpan.FromMinutes(2), null));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var listingStatusConverter = new ValueConverter<Status, string>(
                status => status == Status.NOT_AVAILABLE ? "NOT AVAILABLE" : "AVAILABLE",
                value => string.Equals(value, "NOT AVAILABLE", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "NOT_AVAILABLE", StringComparison.OrdinalIgnoreCase)
                    ? Status.NOT_AVAILABLE
                    : Status.AVAILABLE);
            modelBuilder.Entity<ListingStatus>()
                .Property(status => status.Status)
                .HasConversion(listingStatusConverter);
        }
    }
}

