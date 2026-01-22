using Microsoft.Extensions.Configuration;

namespace ProductCheckerBack
{
    internal class Configuration
    {
        static readonly ConfigurationBuilder _configurationBuilder = new ConfigurationBuilder();
        static readonly IConfiguration _configuration;

        static Configuration()
        {
            _configurationBuilder.AddJsonFile("appSettings.json");
            _configuration = _configurationBuilder.Build();
        }

        public static string GetConnectionString(string dbContext = "ArtemisDbContext")
        {
            return _configuration.GetConnectionString(dbContext);
        }

        public static string GetEvidencePath()
        {
            return _configuration.GetSection("EvidencePath").Value;
        }

        public static int GetRefresh()
        {
            return Convert.ToInt32(_configuration.GetSection("Refresh").Value);
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

        public static string GetProductCheckerApiBaseUrl()
        {
            return _configuration.GetSection("PRODUCT_CHECKER:BaseUrl").Value;
        }
    }
}
