using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using ProductCheckerBack.Artemis.Api;

namespace ProductCheckerBack.Artemis
{
    internal class ArtemisClient
    {
        private static readonly HttpClient _httpClient;
        private static ArtemisClient _instance { get; set; }

        public AuthenticationApi AuthenticationApi { get; private set; }
        public DefaultValuesApi DefaultValuesApi { get; private set; }

        static ArtemisClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(Configuration.GetArtemisApiBaseUrl())
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                Instance.AuthenticationApi.Login(
                    Configuration.GetArtemisLoginUsername(),
                    Configuration.GetArtemisLoginPassword()
                ).Result.Meta.AccessToken
            );
            _httpClient.DefaultRequestHeaders.Add("Gui", "Verification");
        }

        private ArtemisClient()
        {
            AuthenticationApi = new AuthenticationApi(_httpClient);
            DefaultValuesApi = new DefaultValuesApi(_httpClient);
        }

        public static ArtemisClient Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ArtemisClient();

                return _instance;
            }
        }
    }
}
