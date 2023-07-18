using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerService.ServiceBus.Contracts;

namespace WorkerService.ServiceBus.Helper
{
    public class ApiHelper : IApiHelper
    {
        private readonly ILogger<ApiHelper> _logger;
        private readonly AppSettingsModel _setting;
        public ApiHelper(ILogger<ApiHelper> logger, AppSettingsModel settings)
        {
            _logger = logger;
            _setting = settings;
        }


        public async Task<string> InvokeApi(string action, string apiURL, string apiMethod, string JsonRequestContent)
        {
            string _apiResponse = string.Empty;
            double _apiConnectionTimeOut = Convert.ToDouble(_setting.ApiConnectionTimeOut);
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = System.TimeSpan.FromSeconds(Convert.ToDouble(_apiConnectionTimeOut));
                    client.BaseAddress = new Uri(apiURL);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    StringContent theContent = new StringContent(JsonRequestContent, UnicodeEncoding.UTF8, "application/json");
                    HttpResponseMessage response = null;
                    switch (action.ToUpper())
                    {
                        case "POST":
                            response = await client.PostAsync(apiURL + apiMethod, theContent);
                            break;
                        case "GET":
                            response = await client.GetAsync(apiMethod);
                            break;
                    }
                    if (response != null && response.Content != null)
                    {
                        _apiResponse = await response.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _apiResponse = "CallApi method: failed with message: " + ex.Message.ToString();
            }
            return _apiResponse;
        }
    }
}
