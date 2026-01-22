using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductCheckerBack.Services
{
    internal class CountryMapper
    {
        private readonly Dictionary<string, string> _countryShortCodes;

        public CountryMapper()
        {
            _countryShortCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"South Korea", "KR" },
                {"United States", "US"}
            };
        }

        public string GetCountryShortCode(string countryName)
        {
            if (_countryShortCodes.TryGetValue(countryName, out var shortCode))
            {
                return shortCode;
            }

            return countryName;
        }
    }
}
