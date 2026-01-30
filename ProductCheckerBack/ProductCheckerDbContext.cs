using ProductCheckerBack.Models.ProductChecker;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Text.Json;

namespace ProductCheckerBack
{
    internal class ProductCheckerDbContext : DbContext
    {
        public DbSet<ApiEndpoint> ApiEndpoints { get; set; }
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<Port> Ports { get; set; }
        public DbSet<ProductListings> ProductListings { get; set; }
        public DbSet<ProductCheckerBack.Models.ProductChecker.Request> Requests { get; set; }
        public DbSet<RequestInfo> RequestInfos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL(Configuration.GetConnectionString("ProductCheckerDbContext"),
                options => options.EnableRetryOnFailure(5, TimeSpan.FromMinutes(2), null));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var errorsConverter = new ValueConverter<IList<string>?, string?>(
                errors => SerializeErrors(errors),
                errors => DeserializeErrors(errors));

            var errorsComparer = new ValueComparer<IList<string>?>(
                (left, right) => left != null && right != null && left.SequenceEqual(right),
                errors => errors == null ? 0 : errors.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                errors => errors == null ? null : errors.ToList());

            var requestStatusConverter = new EnumToStringConverter<RequestStatus>();

            modelBuilder.Entity<ProductCheckerBack.Models.ProductChecker.Request>()
                .Property(r => r.Errors)
                .HasConversion(errorsConverter)
                .Metadata.SetValueComparer(errorsComparer);

            modelBuilder.Entity<ProductCheckerBack.Models.ProductChecker.Request>()
                .Property(r => r.Status)
                .HasConversion(requestStatusConverter);
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
