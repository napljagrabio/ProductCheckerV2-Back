using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductCheckerBack.Models;

namespace ProductCheckerBack.ExecutionState.DefaultStateHandler
{
    internal sealed class ScanResultSaver
    {
        private readonly ArtemisDbContext _dbContext;
        private readonly Dictionary<long, ExecutionListing> _listingById;

        public ScanResultSaver(ArtemisDbContext dbContext, Dictionary<long, ExecutionListing> listingById)
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
                        _dbContext.ExecutionListings.Attach(listing);
                    }

                    var checkedDate = result.Status?.DateChecked;
                    listing.CheckedDate = string.IsNullOrWhiteSpace(checkedDate)
                        ? DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss")
                        : checkedDate;

                    if (result.Error != null || result.Status == null || !string.IsNullOrWhiteSpace(result.Status?.ErrorDetails))
                    {
                        listing.UrlStatus = "Error";
                        listing.ErrorDetail = result.Error?.ToString() ?? result.Status?.ErrorDetails ?? "";
                        listing.Note = GetUserFriendlyErrorNote(result);
                        _dbContext.SaveChanges();

                        Logger.Log(
                            new ErrorLogging.Payload
                            {
                                ExecutionId = listing.ExecutionId,
                                ListingId = listing.ListingId
                            },
                            $"Product checker scan failed for listing {listing.ListingId}.",
                            listing.ErrorDetail);
                        continue;
                    }

                    listing.UrlStatus = result.Status.Availability ? "Available" : "Not Available";
                    listing.ErrorDetail = result.Status.ErrorDetails;
                    listing.Note = result.Status.Notes;

                    var previousStatus = AddListingStatus(listing.ListingId, result.Status.Availability);
                    _dbContext.SaveChanges();

                    var currentStatus = result.Status.Availability ? Status.AVAILABLE : Status.NOT_AVAILABLE;
                    if (previousStatus != currentStatus)
                    {
                        TryAddGeneralHistory(listing, previousStatus, currentStatus);
                    }
                }
            });
        }

        private Status? AddListingStatus(long listingId, bool availability)
        {
            var status = availability ? Status.AVAILABLE : Status.NOT_AVAILABLE;
            var previousStatus = _dbContext.ListingStatus
                .Where(item => item.ListingId == listingId)
                .OrderByDescending(item => item.Id)
                .Select(item => (Status?)item.Status)
                .FirstOrDefault();

            _dbContext.ListingStatus.Add(new ListingStatus
            {
                ListingId = listingId,
                Status = status,
                CheckedByProductChecker = 1
            });

            return previousStatus;
        }

        private static void TryAddGeneralHistory(
            ExecutionListing listing,
            Status? previousStatus,
            Status currentStatus)
        {
            try
            {
                using var db = new ArtemisDbContext();
                var statusText = GetStatusText(currentStatus);
                var historyText = GetHistoryText(previousStatus, currentStatus, statusText);
                db.GeneralHistory.Add(new GeneralHistory
                {
                    UiType = "admin",
                    ListingId = (ulong)listing.ListingId,
                    UserId = 1068,
                    RauserId = 0,
                    Action = previousStatus.HasValue ? "update" : "insert",
                    Field = "listing_status.status",
                    Value = statusText,
                    Text = historyText,
                    CreatedAt = GetCurrentSingaporeTime()
                });
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.Log(
                    new ErrorLogging.Payload
                    {
                        ExecutionId = listing.ExecutionId,
                        ListingId = listing.ListingId
                    },
                    $"Failed to add general history for listing {listing.ListingId}.",
                    ex.ToString());
            }
        }

        private static string GetHistoryText(Status? previousStatus, Status currentStatus, string statusText)
        {
            if (!previousStatus.HasValue)
            {
                return $"[Product Checker] Added <b>Listing Status</b> with the value of <b>{statusText}</b>";
            }

            return $"[Product Checker] Updated <b>Listing Status</b> from <b>{GetStatusText(previousStatus.Value)}</b> to <b>{statusText}</b>";
        }

        private static DateTime GetCurrentSingaporeTime()
        {
            var current = DateTime.UtcNow.AddHours(8);
            return new DateTime(
                current.Year,
                current.Month,
                current.Day,
                current.Hour,
                current.Minute,
                current.Second,
                DateTimeKind.Unspecified);
        }

        private static string GetStatusText(Status status)
        {
            return status == Status.NOT_AVAILABLE ? "NOT AVAILABLE" : "AVAILABLE";
        }

        private static string GetUserFriendlyErrorNote(ScanTaskResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                return result.ErrorMessage;
            }

            if (!string.IsNullOrWhiteSpace(result.Status?.Notes))
            {
                return result.Status.Notes;
            }

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                return "[Product Checker] Unable to check this listing at the moment.";
            }

            return "[Product Checker] Unable to check this listing at the moment.";
        }
    }
}
