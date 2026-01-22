using Microsoft.EntityFrameworkCore;
using ProductCheckerBack.Models;

namespace ProductCheckerBack
{
    internal class ArtemisDbContext : BaseDbContext
    {
        public DbSet<ListingStatus> ListingStatus { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL(Configuration.GetConnectionString(),
                options => options.EnableRetryOnFailure(5, TimeSpan.FromMinutes(2), null));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
