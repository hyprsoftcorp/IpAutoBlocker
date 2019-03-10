# IP Auto Blocker
Automatically adds Azure App Service network access restrictions based on customizable filtering of standard HTTP server web logs.  By default, the library is configured to add an Azure App Service access restriction rule for each IP address that exceeds 25 404s in a 24-hour period.  The "rules" are user configurable.

## Process Overview
The library periodically performs the following functions:
1. Downloads HTTP server web logs via FTPS.
2. Parses HTTP server web logs.
4. Caches offending HTTP requests based on user defined criteria for a user defined amount of time.
5. Adds App Service access restriction rules for cached offending HTTP requests based on user defined criteria.

## Required Azure Configurations
1. FTPS must be enabled in the app service (FTPS Only recommended).
2. Web server logging to file system must be enabled in the app service.
3. Since the library uses the Azure Management REST API, an Azure active directory app registration must be created and configured.

## App Settings
Setting | Sample Value | Description
--- | --- | ---
IpAutoBlockerSettings:ClientId | 8a171fc2-f71f-4eb3-95fd-e4e5da70f8d2 | Azure service principal client identifier.
IpAutoBlockerSettings:ClientSecret | cl13ntS3cr3t | Azure service principal client secret.
IpAutoBlockerSettings:Tenant | edb1b40-6058-439e-a656-46d8d02c4645 | Azure tenant.
IpAutoBlockerSettings:SubscriptionId | 29abab24-250f-4227-adcc-ab81b84ae9df | Azure subscription id where website resides.
IpAutoBlockerSettings:WebsiteName | mywebsite | Azure app service name.
FtpHttpLogProviderSettings:Host | waws-prod-bay-011.ftp.azurewebsites.windows.net | FTP host where HTTP logs reside.
FtpHttpLogProviderSettings:Username | mywebsite\\$mywebsite | FTP username.
FtpHttpLogProviderSettings:Password | ftpp@ssw0rd | FTP password.
FtpHttpLogProviderSettings:LogsFolder | /LogFiles/http/RawLogs | FTP remote path where HTTP logs reside.

## Implementations
### Azure Function Sample
This function runs every 8 hours.
```csharp
[FunctionName("IpAutoBlocker")]
public static async Task Run([TimerTrigger("0 0 */8 * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context, CancellationToken token)
{
    var config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

    var blocker = new Hyprsoft.Cloud.Utilities.Azure.IpAutoBlocker(log, new IpAutoBlockerSettings
    {
        ClientId = config["Values:IpAutoBlockerSettings:ClientId"],
        ClientSecret = config["Values:IpAutoBlockerSettings:ClientSecret"],
        SubscriptionId = config["Values:IpAutoBlockerSettings:SubscriptionId"],
        Tenant = config["Values:IpAutoBlockerSettings:Tenant"],
        WebsiteName = config["Values:IpAutoBlockerSettings:WebsiteName"]
    })
    {
        HttpLogProvider = new FtpHttpLogProvider(new FtpHttpLogProviderSettings
        {
            Host = config["Values:FtpHttpLogProviderSettings:Host"],
            Username = config["Values:FtpHttpLogProviderSettings:Username"],
            Password = config["Values:FtpHttpLogProviderSettings:Password"],
            LogsFolder = config["Values:FtpHttpLogProviderSettings:LogsFolder"]
        })
    };
    await blocker.RunAsync(token);
}
```
### Console App Sample
See the full console app [source code](https://github.com/hyprsoftcorp/IpAutoBlocker/blob/master/Hyprsoft.IpAutoBlocker/Program.cs) for details.
```csharp
static async Task Main(string[] args)
{
    var logger = _loggerFactory.CreateLogger<Program>();
    using (var cts = new CancellationTokenSource())
    {
        Console.WriteLine("\nPress Ctrl+C to exit.\n");
        Console.CancelKeyPress += (s, e) =>
        {
            cts.Cancel();
            e.Cancel = true;
        };
        using (var blocker = new IpAutoBlocker(_loggerFactory.CreateLogger<IpAutoBlocker>(), _settings.IpAutoBlockerSettings)
        {
            HttpLogProvider = new FtpHttpLogProvider(_settings.FtpHttpLogProviderSettings)
        })
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await blocker.RunAsync(cts.Token);
                await Task.Delay(_settings.CheckInterval, cts.Token);
            }   // cancellation pending while loop
        }
    }}
```