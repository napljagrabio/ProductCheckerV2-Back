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
using ProductCheckerBack.Models.ProductChecker;
using ProductCheckerBack.Services;

namespace ProductCheckerBack
{
    internal class ProductCheckerService
    {
        public readonly Request Request;

        private readonly HttpClient _httpClient;
        private readonly ProductCheckerDbContext _productCheckerDbContext;

        public ProductCheckerService(Request request, ProductCheckerDbContext dbContext, HttpClient httpClient = null)
        {
            Request = request;
            _productCheckerDbContext = dbContext;
            _httpClient = httpClient ?? new HttpClient();
        }

#nullable disable

        public void MarkAsPending(bool forcedPendingCauseOfPriority = false)
        {
            if (forcedPendingCauseOfPriority) 
            {
                Request.RescanInfoId = 1;
            }
            Request.Status = RequestStatus.PENDING;
            Request.RequestEnded = null;
            _productCheckerDbContext.SaveChanges();
        }

        public void MarkAsProcessing()
        {
            Request.Status = RequestStatus.PROCESSING;
            Request.RequestEnded = null;
            _productCheckerDbContext.SaveChanges();
        }

        public void MarkAsCompletedWithIssues(List<string> errors)
        {
            Request.Errors = errors;
            Request.Status = RequestStatus.COMPLETED_WITH_ISSUES;
            Request.RequestEnded = DateTime.UtcNow.AddHours(8); // Philippine Standard Time

            _productCheckerDbContext.SaveChanges();
        }

        public void MarkAsFailed(List<string> errors)
        {
            Request.Errors = errors;
            Request.Status = RequestStatus.FAILED;
            Request.RequestEnded = DateTime.UtcNow.AddHours(8); // Philippine Standard Time

            _productCheckerDbContext.SaveChanges();
        }

        public void MarkAsSuccess()
        {
            Request.Status = RequestStatus.SUCCESS;
            Request.RequestEnded = DateTime.UtcNow.AddHours(8); // Philippine Standard Time

            _productCheckerDbContext.SaveChanges();
        }

        public List<ProductListings> GetAllProductListings()
        {
            EnsureRequestListingsLoaded();

            return Request?
                .RequestInfo?
                .ProductListings?
                .ToList() ?? new List<ProductListings>();
        }

        public List<ProductListings> GetErrorProductListings()
        {
            EnsureRequestListingsLoaded();

            var errorStatuses = new HashSet<string> { "Not Available", "Available" };

            return Request?
                .RequestInfo?
                .ProductListings?
                .Where(listing => listing != null && !errorStatuses.Contains(listing.UrlStatus))
                .ToList() ?? new List<ProductListings>();
        }

        public List<ProductListings> GetOrganizedListings(bool onlyErrors = false)
        {
            EnsureRequestListingsLoaded();

            var listings = new List<ProductListings>();
            if (onlyErrors || Request.RescanInfoId == 1)
            {
                listings = GetErrorProductListings();
            }
            else
            {
                listings = GetAllProductListings();
            }

            return ProductListingQueueBuilder.BuildRoundRobinByPlatform(listings);
        }

        private void EnsureRequestListingsLoaded()
        {
            if (Request.RequestInfo == null)
            {
                _productCheckerDbContext.Entry(Request)
                    .Reference(r => r.RequestInfo)
                    .Load();
            }

            if (Request.RequestInfo != null)
            {
                _productCheckerDbContext.Entry(Request.RequestInfo)
                    .Collection(ri => ri.ProductListings)
                    .Load();
            }
        }
    }
}
