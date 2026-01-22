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
        private readonly ProductCheckerV2DbContext _productCheckerV2DbContext;

        public ProductCheckerService(Request request, ProductCheckerV2DbContext dbContext, HttpClient httpClient = null)
        {
            Request = request;
            _productCheckerV2DbContext = dbContext;
            _httpClient = httpClient ?? new HttpClient();
        }

#nullable disable

        public void MarkAsProcessing()
        {
            Request.Status = RequestStatus.PROCESSING;
            Request.RequestEnded = null;
            _productCheckerV2DbContext.SaveChanges();
        }

        public void MarkAsCompletedWithIssues(List<string> errors)
        {
            Request.Errors = errors;
            Request.Status = RequestStatus.COMPLETED_WITH_ISSUES;
            Request.RequestEnded = DateTime.UtcNow.AddHours(8); // Philippine Standard Time

            _productCheckerV2DbContext.SaveChanges();
        }

        public void MarkAsFailed(List<string> errors)
        {
            Request.Errors = errors;
            Request.Status = RequestStatus.FAILED;
            Request.RequestEnded = DateTime.UtcNow.AddHours(8); // Philippine Standard Time

            _productCheckerV2DbContext.SaveChanges();
        }

        public void MarkAsSuccess()
        {
            Request.Status = RequestStatus.SUCCESS;
            Request.RequestEnded = DateTime.UtcNow.AddHours(8); // Philippine Standard Time

            _productCheckerV2DbContext.SaveChanges();
        }

        public int GetProductListingsCount()
        {
            EnsureRequestListingsLoaded();
            return Request.RequestInfo?.ProductListings?.Count ?? 0;
        }

        public List<ProductListings> GetOrganizedListings()
        {
            EnsureRequestListingsLoaded();
            var listings = Request.RequestInfo?.ProductListings?.ToList() ?? new List<ProductListings>();
            return ProductListingQueueBuilder.BuildRoundRobinByPlatform(listings);
        }

        private void EnsureRequestListingsLoaded()
        {
            if (Request.RequestInfo == null)
            {
                _productCheckerV2DbContext.Entry(Request)
                    .Reference(r => r.RequestInfo)
                    .Load();
            }

            if (Request.RequestInfo != null)
            {
                _productCheckerV2DbContext.Entry(Request.RequestInfo)
                    .Collection(ri => ri.ProductListings)
                    .Load();
            }
        }
    }
}
