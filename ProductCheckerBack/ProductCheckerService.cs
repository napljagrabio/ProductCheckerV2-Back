using ProductCheckerBack.Artemis;
using ProductCheckerBack.Models.Logging;
using ProductCheckerBack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Net.Http;
using ProductCheckerBack.Services;
using Microsoft.EntityFrameworkCore;

namespace ProductCheckerBack
{
    internal class ProductCheckerService
    {
        public readonly Execution Execution;

        private readonly HttpClient _httpClient;
        private readonly ArtemisDbContext _productCheckerDbContext;

        public ProductCheckerService(Execution execution, ArtemisDbContext dbContext, HttpClient httpClient = null)
        {
            Execution = execution;
            _productCheckerDbContext = dbContext;
            _httpClient = httpClient ?? new HttpClient();
        }

#nullable disable

        public void MarkAsPending()
        {
            Execution.Status = ExecutionStatus.PENDING;
            Execution.ExecutionEnded = null;
            _productCheckerDbContext.SaveChanges();
        }

        public void MarkAsProcessing()
        {
            Execution.Status = ExecutionStatus.PROCESSING;
            Execution.ExecutionStarted = DateTime.UtcNow.AddHours(8); // Philippine Standard Time
            Execution.ExecutionEnded = null;
            _productCheckerDbContext.SaveChanges();
        }

        public void MarkAsCompletedWithIssues(List<string> errors)
        {
            Execution.Errors = errors;
            Execution.Status = ExecutionStatus.COMPLETED_WITH_ISSUES;
            Execution.ExecutionEnded = DateTime.UtcNow.AddHours(8); // Philippine Standard Time

            _productCheckerDbContext.SaveChanges();
        }

        public void MarkAsFailed(List<string> errors)
        {
            Execution.Errors = errors;
            Execution.Status = ExecutionStatus.FAILED;
            Execution.ExecutionEnded = DateTime.UtcNow.AddHours(8); // Philippine Standard Time

            _productCheckerDbContext.SaveChanges();
        }

        public void MarkAsSuccess()
        {
            Execution.Status = ExecutionStatus.SUCCESS;
            Execution.ExecutionEnded = DateTime.UtcNow.AddHours(8); // Philippine Standard Time

            _productCheckerDbContext.SaveChanges();
        }

        public List<ExecutionListing> GetAllExecutionListings()
        {
            EnsureExecutionListingsLoaded();

            return Execution?
                .ExecutionListings?
                .ToList() ?? new List<ExecutionListing>();
        }

        public List<ExecutionListing> GetErrorExecutionListings()
        {
            EnsureExecutionListingsLoaded();

            var errorStatuses = new HashSet<string> { "Not Available", "Available" };

            return Execution?
                .ExecutionListings?
                .Where(listing => listing != null && !errorStatuses.Contains(listing.UrlStatus))
                .ToList() ?? new List<ExecutionListing>();
        }

        public List<ExecutionListing> GetOrganizedListings(bool onlyErrors = false)
        {
            EnsureExecutionListingsLoaded();

            var listings = new List<ExecutionListing>();
            if (onlyErrors)
            {
                listings = GetErrorExecutionListings();
            }
            else
            {
                listings = GetAllExecutionListings();
            }

            return ExecutionListingQueueBuilder.BuildRoundRobinByPlatform(listings);
        }

        private void EnsureExecutionListingsLoaded()
        {
            if (!_productCheckerDbContext.Entry(Execution)
                .Collection(execution => execution.ExecutionListings).IsLoaded)
            {
                _productCheckerDbContext.Entry(Execution)
                    .Collection(execution => execution.ExecutionListings)
                    .Query()
                    .Include(result => result.Listing)
                    .ThenInclude(listing => listing.Platform)
                    .Load();
            }
        }
    }
}
