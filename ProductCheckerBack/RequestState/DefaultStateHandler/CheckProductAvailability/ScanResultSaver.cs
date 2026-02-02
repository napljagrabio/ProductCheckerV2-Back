using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductCheckerBack.Models.ProductChecker;

namespace ProductCheckerBack.RequestState.DefaultStateHandler
{
    internal sealed class ScanResultSaver
    {
        private readonly ProductCheckerDbContext _dbContext;
        private readonly Dictionary<int, ProductListings> _listingById;

        public ScanResultSaver(ProductCheckerDbContext dbContext, Dictionary<int, ProductListings> listingById)
        {
            _dbContext = dbContext;
            _listingById = listingById;
        }

        public Task RunAsync(BlockingCollection<ScanTaskResult> results)
        {
            return Task.Run(() =>
            {
                foreach (var result in results.GetConsumingEnumerable())
                {
                    if (!_listingById.TryGetValue(result.ListingDbId, out var listing))
                    {
                        continue;
                    }

                    if (_dbContext.Entry(listing).State == EntityState.Detached)
                    {
                        _dbContext.ProductListings.Attach(listing);
                    }

                    var checkedDate = result.Status?.DateChecked;
                    listing.CheckedDate = string.IsNullOrWhiteSpace(checkedDate)
                        ? DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss")
                        : checkedDate;

                    if (result.Error != null || result.Status == null || !string.IsNullOrWhiteSpace(result.Status?.ErrorDetails))
                    {
                        listing.UrlStatus = "Error";
                        listing.ErrorDetail = result.Error?.ToString() ?? result.Status?.ErrorDetails ?? "";
                        listing.Note = result.ErrorMessage
                            ?? result.Status?.Notes
                            ?? TryGetErrorNote(result.Error)
                            ?? "Server Request Error";
                        _dbContext.SaveChanges();
                        continue;
                    }

                    listing.UrlStatus = result.Status.Availability ? "Available" : "Not Available";
                    listing.ErrorDetail = result.Status.ErrorDetails;
                    listing.Note = result.Status.Notes;
                    _dbContext.SaveChanges();
                }
            });
        }

        private static string? TryGetErrorNote(string? error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return null;
            }

            var firstLine = error.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .FirstOrDefault();
            return string.IsNullOrWhiteSpace(firstLine) ? null : firstLine.Trim();
        }
    }
}
