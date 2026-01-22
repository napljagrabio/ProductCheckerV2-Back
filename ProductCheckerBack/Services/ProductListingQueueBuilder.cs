using System;
using System.Collections.Generic;
using System.Linq;
using ProductCheckerBack.Models.ProductChecker;

namespace ProductCheckerBack.Services
{
    internal static class ProductListingQueueBuilder
    {
        public static List<ProductListings> BuildRoundRobinByPlatform(IEnumerable<ProductListings> listings)
        {
            if (listings == null)
            {
                return new List<ProductListings>();
            }

            var allListings = listings.Where(listing => listing != null).ToList();

            var groupedListings = allListings
                .Where(listing => !string.IsNullOrWhiteSpace(listing.Platform))
                .GroupBy(listing => listing.Platform.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            var platformOrder = groupedListings.Keys
                .OrderBy(platform => platform, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var roundRobinListings = new List<ProductListings>(allListings.Count);
            var index = 0;
            var hasMoreListings = true;

            while (hasMoreListings)
            {
                hasMoreListings = false;
                foreach (var platform in platformOrder)
                {
                    var platformListings = groupedListings[platform];
                    if (index < platformListings.Count)
                    {
                        roundRobinListings.Add(platformListings[index]);
                        hasMoreListings = true;
                    }
                }

                index++;
            }

            roundRobinListings.AddRange(allListings.Where(listing => string.IsNullOrWhiteSpace(listing.Platform)));

            return roundRobinListings;
        }
    }
}
