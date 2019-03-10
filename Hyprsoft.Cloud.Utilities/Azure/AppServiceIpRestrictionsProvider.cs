using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.Azure
{
    public class AppServiceIpRestrictionsProvider : IpRestrictionsProvider
    {
        #region Fields

        private bool _isDisposed;

        #endregion

        #region Properties

        private const string ApiVersion = "2018-02-01";

        private readonly IpAutoBlockerSettings _settings;
        private HttpClient _httpClient;
        private WebApp _webAppConfiguration;

        #endregion

        #region Constructors

        public AppServiceIpRestrictionsProvider(IpAutoBlockerSettings settings)
        {
            _settings = settings;
        }

        #endregion

        #region Properties

        public const string IpAddressBlockSuffix = "/32";

        #endregion

        #region Methods

        protected async override Task<List<IpRestriction>> OnGetRestrictionsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await CreateHttpClientAsync();
            await GetWebAppConfigurationAsync();

            // Give back a copy so the caller can maintain their own list and we can free some memory and reduce the PUT payload size.
            var restrictions = _webAppConfiguration.Properties.Restrictions.ToList();
            _webAppConfiguration.Properties.Restrictions.Clear();
            return restrictions;
        }

        protected async override Task OnAddRestrictionsAsync(IEnumerable<IpRestriction> restrictions, CancellationToken cancellationToken = default(CancellationToken))
        {
            await CreateHttpClientAsync();
            await GetWebAppConfigurationAsync();

            _webAppConfiguration.Properties.Restrictions.AddRange(restrictions);

            var response = await _httpClient.PutAsync($"https://management.azure.com/{_webAppConfiguration.Id}?api-version={ApiVersion}",
                new StringContent(JsonConvert.SerializeObject(_webAppConfiguration), Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Submitting IP restrictions failed with status '{response.ReasonPhrase}'.  Details: {await response.Content.ReadAsStringAsync().ConfigureAwait(false) ?? "none"}");
        }

        private async Task CreateHttpClientAsync()
        {
            if (_httpClient != null)
                return;

            Logger.LogInformation("Authenticating with Azure Managment API.");
            _httpClient = new HttpClient();
            var form = new Dictionary<string, string>
            {
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret },
                { "resource", "https://management.core.windows.net/" },
                { "grant_type", "client_credentials" }
            };
            var response = await _httpClient.PostAsync($"https://login.microsoftonline.com/{_settings.Tenant}/oauth2/token", new FormUrlEncodedContent(form)).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Authentication failed with status '{response.ReasonPhrase}'.  Details: {await response.Content.ReadAsStringAsync().ConfigureAwait(false) ?? "none"}");
            var auth = JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync().ConfigureAwait(false), new { access_token = "" });

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.access_token);
        }

        private async Task GetWebAppConfigurationAsync()
        {
            if (_webAppConfiguration != null)
                return;

            Logger.LogInformation($"Getting web apps for subscription '{_settings.SubscriptionId}'.");

            // Get our website's resource id.
            var response = await _httpClient.GetAsync($"https://management.azure.com/subscriptions/{_settings.SubscriptionId}/providers/Microsoft.Web/sites?api-version={ApiVersion}").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Getting website's resource id failed with status '{response.ReasonPhrase}'.  Details: {await response.Content.ReadAsStringAsync().ConfigureAwait(false) ?? "none"}");
            var webAppsList = JsonConvert.DeserializeObject<WebAppsList>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

            var webSiteResourceId = webAppsList.WebApps.FirstOrDefault(w => String.Compare(w.Name, _settings.WebsiteName, true) == 0)?.Id;

            Logger.LogInformation($"Getting web app configuration for '{_settings.WebsiteName}'.");

            // Get our website's ip restrictions.
            response = await _httpClient.GetAsync($"https://management.azure.com/{webSiteResourceId}/config/web?api-version={ApiVersion}").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Getting IP restrictions failed with status 'response.ReasonPhrase'.  Details: {await response.Content.ReadAsStringAsync().ConfigureAwait(false) ?? "none"}");

            _webAppConfiguration = JsonConvert.DeserializeObject<WebApp>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_isDisposed)
                return;

            // Managed resources.
            if (disposing)
                _httpClient?.Dispose();

            // Unmanaged resources.

            _isDisposed = true;
        }

        #endregion
    }
}
