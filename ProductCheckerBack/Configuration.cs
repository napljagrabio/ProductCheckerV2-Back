using Microsoft.Extensions.Configuration;

namespace ProductCheckerBack
{
    internal static class Configuration
    {
        private static readonly IConfiguration ConfigurationRoot = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
            .Build();

        public static string GetConnectionString(string dbContext = "ArtemisDbContext")
        {
            return GetRequiredValue($"ConnectionStrings:{dbContext}");
        }

        public static string GetBaseUrlConnectionString() => GetRequiredValue("ARTEMIS:BaseUrl");

        public static string GetArtemisConnectionString() =>
            GetRequiredValue("ConnectionStrings:ArtemisDbContext");

        public static string GetLoggingConnectionString() =>
            GetRequiredValue("ConnectionStrings:LoggingDbContext");

        public static int GetRefresh() => GetIntValue("Refresh", 20000);

        public static int GetClearStorageThreshold() => GetIntValue("ClearStorageThreshold", 200);

        public static string GetToolName() => "Product Checker";

        public static string GetArtemisLoginUsername() =>
            GetRequiredValue("ARTEMIS:Credentials:Username");

        public static string GetArtemisLoginPassword() =>
            GetRequiredValue("ARTEMIS:Credentials:Password");

        private static string GetRequiredValue(string key)
        {
            var value = ConfigurationRoot[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Configuration value '{key}' is missing.");
            }

            return value;
        }

        private static int GetIntValue(string key, int defaultValue)
        {
            var value = ConfigurationRoot[key];
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
    }
}
