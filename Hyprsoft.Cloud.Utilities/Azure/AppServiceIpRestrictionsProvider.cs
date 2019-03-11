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

        private const string ApiVersion = "2018-02-01";

        private bool _isDisposed;
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

        public const string IpAddressCidrBlockSuffix = "/32";

        #endregion

        #region Methods

        protected async override Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation($"Authenticating with Azure Managment REST API using client id '{_settings.ClientId}'.");

            _httpClient = new HttpClient();
            var form = new Dictionary<string, string>
            {
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret },
                { "resource", "https://management.core.windows.net/" },
                { "grant_type", "client_credentials" }
            };
            var response = await _httpClient.PostAsync($"https://login.microsoftonline.com/{_settings.Tenant}/oauth2/token", new FormUrlEncodedContent(form), cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Authentication failed with status '{response.ReasonPhrase}'.  Details: {await response.Content.ReadAsStringAsync().ConfigureAwait(false) ?? "none"}");
            var auth = JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync().ConfigureAwait(false), new { access_token = "" });

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.access_token);

            await base.OnInitializeAsync(cancellationToken);
        }

        protected async override Task OnLoadAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation($"Getting web apps for subscription '{_settings.SubscriptionId}'.");

            // Get our website's resource id.
            var response = await _httpClient.GetAsync($"https://management.azure.com/subscriptions/{_settings.SubscriptionId}/providers/Microsoft.Web/sites?api-version={ApiVersion}", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Getting website's resource id failed with status '{response.ReasonPhrase}'.  Details: {await response.Content.ReadAsStringAsync().ConfigureAwait(false) ?? "none"}");
            var webAppsList = JsonConvert.DeserializeObject<WebAppsList>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

            Logger.LogInformation($"Getting IP restrictions for '{_settings.WebsiteName}'.");
            var webSiteResourceId = webAppsList.WebApps.FirstOrDefault(w => String.Compare(w.Name, _settings.WebsiteName, true) == 0)?.Id;
            if (webSiteResourceId == null)
                throw new InvalidOperationException($"The '{_settings.WebsiteName}' website was not found in Azure subscription '{_settings.SubscriptionId}'.");

            // Get our website's ip restrictions.
            response = await _httpClient.GetAsync($"https://management.azure.com/{webSiteResourceId}/config/web?api-version={ApiVersion}", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Getting IP restrictions failed with status 'response.ReasonPhrase'.  Details: {await response.Content.ReadAsStringAsync().ConfigureAwait(false) ?? "none"}");

            _webAppConfiguration = JsonConvert.DeserializeObject<WebApp>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

            Restrictions.Clear();
            Restrictions.AddRange(_webAppConfiguration.Properties.Restrictions);
        }

        protected async override Task OnSaveAsync(CancellationToken cancellationToken)
        {
            _webAppConfiguration.Properties.Restrictions.Clear();
            _webAppConfiguration.Properties.Restrictions.AddRange(Restrictions);

            var response = await _httpClient.PutAsync($"https://management.azure.com/{_webAppConfiguration.Id}?api-version={ApiVersion}",
                new StringContent(JsonConvert.SerializeObject(_webAppConfiguration), Encoding.UTF8, "application/json"), cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Submitting IP restrictions failed with status '{response.ReasonPhrase}'.  Details: {await response.Content.ReadAsStringAsync().ConfigureAwait(false) ?? "none"}");
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
