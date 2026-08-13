using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProductCheckerBack.Models;
using System.Text.Json;

namespace ProductCheckerBack
{
    internal class ArtemisDbContext : BaseDbContext
    {
        public DbSet<ListingStatus> ListingStatus { get; set; }
        public DbSet<GeneralHistory> GeneralHistory { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ApiEndpoint> ApiEndpoints { get; set; }
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<Port> Ports { get; set; }
        public DbSet<ExecutionListing> ExecutionListings { get; set; }
        public DbSet<Models.Execution> Executions { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<Case> Cases { get; set; }

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

            var errorsConverter = new ValueConverter<IList<string>?, string?>(
                errors => SerializeErrors(errors),
                errors => DeserializeErrors(errors));

            var errorsComparer = new ValueComparer<IList<string>?>(
                (left, right) => ReferenceEquals(left, right) ||
                    (left != null && right != null && left.SequenceEqual(right)),
                errors => errors == null ? 0 : errors.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                errors => errors == null ? null : errors.ToList());

            var executionStatusConverter = new EnumToStringConverter<ExecutionStatus>();

            modelBuilder.Entity<ListingStatus>()
                .Property(status => status.Status)
                .HasConversion(listingStatusConverter);

            modelBuilder.Entity<Models.Execution>()
                .Property(r => r.Errors)
                .HasConversion(errorsConverter)
                .Metadata.SetValueComparer(errorsComparer);

            modelBuilder.Entity<Models.Execution>()
                .Property(r => r.Status)
                .HasConversion(executionStatusConverter);
        }

        private static string? SerializeErrors(IList<string>? errors)
        {
            return errors == null ? null : JsonSerializer.Serialize(errors);
        }

        private static IList<string> DeserializeErrors(string? errors)
        {
            if (string.IsNullOrWhiteSpace(errors))
            {
                return new List<string>();
            }

            return JsonSerializer.Deserialize<List<string>>(errors) ?? new List<string>();
        }
    }
}

