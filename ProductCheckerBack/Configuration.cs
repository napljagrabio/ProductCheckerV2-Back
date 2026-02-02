using Microsoft.Extensions.Configuration;
using System.Threading;

namespace ProductCheckerBack
{
    internal class Configuration
    {
        static readonly ConfigurationBuilder _configurationBuilder = new ConfigurationBuilder();
        static readonly IConfiguration _configuration;
        private static readonly AsyncLocal<string?> _currentEnvironment = new AsyncLocal<string?>();
        private const string EnvironmentStage = "Stage";
        private const string EnvironmentLive = "Live";
        private const string DefaultEnvironment = EnvironmentStage;

        static Configuration()
        {
            _configurationBuilder.AddJsonFile("appSettings.json");
            _configuration = _configurationBuilder.Build();
        }

        public static string GetConnectionString(string dbContext = "ProductDbContext")
        {
            return _configuration.GetConnectionString(dbContext);
        }

        public static void SetCurrentEnvironment(string? environment)
        {
            _currentEnvironment.Value = NormalizeEnvironment(environment);
        }

        public static string GetCurrentEnvironment()
        {
            return _currentEnvironment.Value ?? DefaultEnvironment;
        }

        public static string GetArtemisConnectionStringName()
        {
            return GetCurrentEnvironment() == EnvironmentLive
                ? "Live"
                : "Stage";
        }

        public static string GetBaseUrlConnectionString()
        {
            return GetCurrentEnvironment() == EnvironmentLive
                ? GetBaseUrlLiveConnectionString()
                : GetBaseUrlStageConnectionString();
        }

        public static string GetArtemisConnectionString()
        {
            return GetCurrentEnvironment() == EnvironmentLive
                ? GetArtemisLiveConnectionString()
                : GetArtemisStageConnectionString();
        }

        public static string GetLoggingConnectionString()
        {
            return GetCurrentEnvironment() == EnvironmentLive
                ? GetLoggingLiveConnectionString()
                : GetLoggingStageConnectionString();
        }

        //================== STAGE ENV ==================//

        public static string GetBaseUrlStageConnectionString()
        {
            return _configuration.GetSection("Stage:BaseUrl").Value;
        }

        public static string GetArtemisStageConnectionString()
        {
            return _configuration.GetSection("Stage:ArtemisDbContext").Value;
        }

        public static string GetLoggingStageConnectionString()
        {
            return _configuration.GetSection("Stage:LoggingDbContext").Value;
        }

        //================== LIVE ENV ==================//

        public static string GetBaseUrlLiveConnectionString()
        {
            return _configuration.GetSection("Live:BaseUrl").Value;
        }

        public static string GetArtemisLiveConnectionString()
        {
            return _configuration.GetSection("Live:ArtemisDbContext").Value;
        }

        public static string GetLoggingLiveConnectionString()
        {
            return _configuration.GetSection("Live:LoggingDbContext").Value;
        }

        //======================== General Settings ========================//

        public static string GetEvidencePath()
        {
            return _configuration.GetSection("EvidencePath").Value;
        }

        public static int GetRefresh()
        {
            return Convert.ToInt32(_configuration.GetSection("Refresh").Value);
        }

        public static int GetClearStorageThreshold()
        {
            return Convert.ToInt32(_configuration.GetSection("ClearStorageThreshold").Value);
        }

        public static string GetToolName()
        {
            return "Product Checker";
        }

        public static string GetArtemisApiBaseUrl()
        {
            return _configuration.GetSection("ARTEMIS:BaseUrl").Value;
        }

        public static string GetArtemisLoginUsername()
        {
            return _configuration.GetSection("ARTEMIS:Credentials:Username").Value;
        }

        public static string GetArtemisLoginPassword()
        {
            return _configuration.GetSection("ARTEMIS:Credentials:Password").Value;
        }

        private static string NormalizeEnvironment(string? environment)
        {
            if (string.Equals(environment, EnvironmentLive, StringComparison.OrdinalIgnoreCase))
            {
                return EnvironmentLive;
            }

            if (string.Equals(environment, EnvironmentStage, StringComparison.OrdinalIgnoreCase))
            {
                return EnvironmentStage;
            }

            return DefaultEnvironment;
        }
    }
}
