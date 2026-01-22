using ProductCheckerBack.Models.ProductChecker;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using ProductCheckerBack.Models;
using ProductCheckerBack;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ProductCheckerBack
{
    internal class ProductCheckerV2DbContext : DbContext
    {
        public DbSet<ProductListings> ProductListings { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<RequestInfo> RequestInfos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL(Configuration.GetConnectionString("ProductCheckerV2DbContext"),
                options => options.EnableRetryOnFailure(5, TimeSpan.FromMinutes(2), null));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure RequestInfo entity
            modelBuilder.Entity<RequestInfo>(entity =>
            {
                entity.ToTable("request_infos");
                entity.HasKey(e => e.Id);

                // Configure properties
                entity.Property(e => e.User)
                    .HasMaxLength(255);
                entity.Property(e => e.FileName)
                    .HasMaxLength(255);
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime");

                // Navigation properties - UPDATED
                entity.HasMany(e => e.Requests)
                    .WithOne(e => e.RequestInfo)
                    .HasForeignKey(e => e.RequestInfoId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Add this configuration for ProductListings
                entity.HasMany(e => e.ProductListings)
                    .WithOne(e => e.RequestInfo)
                    .HasForeignKey(e => e.RequestInfoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Request entity
            modelBuilder.Entity<Request>(entity =>
            {
                entity.ToTable("requests");
                entity.HasKey(e => e.Id);

                // Configure properties
                entity.Property(e => e.Status)
                    .HasConversion<string>() // Store enum as string
                    .HasMaxLength(50);
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime");
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime");
                entity.Property(e => e.RequestEnded)
                    .HasColumnType("datetime");
                entity.Property(e => e.DeletedAt)
                    .HasColumnType("datetime");
                entity.Property(e => e.Errors)
                    .HasConversion(
                        errors => JsonSerializer.Serialize(errors ?? new List<string>(), (JsonSerializerOptions?)null),
                        json => string.IsNullOrWhiteSpace(json)
                            ? new List<string>()
                            : JsonSerializer.Deserialize<IList<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
                    .Metadata.SetValueComparer(
                        new ValueComparer<IList<string>?>(
                            (left, right) => left == null
                                ? right == null
                                : right != null && left.SequenceEqual(right),
                            list => list == null
                                ? 0
                                : list.Aggregate(0, (current, item) => HashCode.Combine(current, item == null ? 0 : item.GetHashCode())),
                            list => list == null ? null : (IList<string>)list.ToList()));
                entity.Property(e => e.Errors).IsRequired(false);

                // Foreign key relationship
                entity.HasOne(e => e.RequestInfo)
                    .WithMany(e => e.Requests)
                    .HasForeignKey(e => e.RequestInfoId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ProductListings entity
            modelBuilder.Entity<ProductListings>(entity =>
            {
                entity.ToTable("product_checker_listings");
                entity.HasKey(e => e.Id);

                // Configure properties
                entity.Property(e => e.ListingId)
                    .HasMaxLength(255);
                entity.Property(e => e.CaseNumber)
                    .HasMaxLength(100);
                entity.Property(e => e.Platform)
                    .HasMaxLength(50);
                entity.Property(e => e.Url)
                    .HasMaxLength(2000);
                entity.Property(e => e.UrlStatus)
                    .HasMaxLength(50);
                entity.Property(e => e.CheckedDate)
                    .HasMaxLength(50);
                entity.Property(e => e.ErrorDetail)
                    .HasColumnType("text");
                entity.Property(e => e.Note)
                    .HasColumnType("text");
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime");

                // Foreign key relationship - UPDATED to match the RequestInfo configuration
                entity.HasOne(e => e.RequestInfo)
                    .WithMany(e => e.ProductListings)  // Now specifies the navigation property
                    .HasForeignKey(e => e.RequestInfoId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
