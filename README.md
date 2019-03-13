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
### IP Auto Blocker
Setting | Description | Default | Sample Value
--- | --- | ---
IpAutoBlockerSettings:ClientId* | Azure service principal client identifier. | Null | 8a171fc2-f71f-4eb3-95fd-e4e5da70f8d2
IpAutoBlockerSettings:ClientSecret* | Azure service principal client secret. | Null | cl13ntS3cr3t 
IpAutoBlockerSettings:Tenant* | Azure tenant. | Null | edb1b40-6058-439e-a656-46d8d02c4645
IpAutoBlockerSettings:SubscriptionId* | Azure subscription id where website resides. | Null | 29abab24-250f-4227-adcc-ab81b84ae9df
IpAutoBlockerSettings:WebsiteName* | Azure app service name. | Null | mywebsite
IpAutoBlockerSettings:SyncInterval | Interval at which the HTTP traffic cache is synched with the app service IP restrictions. | 24-hours  | 1.00:00:00

### FTP HTTP Log Provider
Setting | Description | Default | Sample Value
--- | --- | ---
FtpHttpLogProviderSettings:Host* | FTP host where HTTP logs reside. | Null | waws-prod-bay-011.ftp.azurewebsites.windows.net
FtpHttpLogProviderSettings:Username* | FTP username. | Null| mywebsite\\$mywebsite
FtpHttpLogProviderSettings:Password* | FTP password. | Null | ftpp@ssw0rd
FtpHttpLogProviderSettings:LogsFolder* | FTP remote path where HTTP logs reside. | /LogFiles/http/RawLogs | /LogFiles/mylogs
FtpHttpLogProviderSettings:AutoDeleteLogs | Delete remote HTTP log files once downloaded; otherwise back them up. | false | true

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

### Sample Azure Function Log File
```
1. Executing 'IpAutoBlocker' (Reason='Timer fired at 2019-03-11T21:56:53.8044105-07:00', Id=00000000-0000-0000-0000-000000000000)
2. Trigger Details: UnscheduledInvocationReason: RunOnStartup
3. IP Auto Blocker function triggered at '3/11/2019 9:56:54 PM'.
4. Hyprsoft Cloud Utilites v1.0.0
5. IP Auto Blocker running using: HTTP Log Provider: 'FtpHttpLogProvider' IP Restrictions Provider: 'AppServiceIpRestrictionsProvider' Azure Web App: 'myweb' Sync Interval: '24' hours HTTP Logs Filter: 'entries => entries.Where(entry => (Convert(entry.Status, Int32) == 404))' HTTP Traffic Cache Filter: 'items => items.Where(x => (x.Value >= 25))'
6. Initializing provider 'AppServiceIpRestrictionsProvider'.
7. Authenticating with Azure Managment REST API using client id '00000000-0000-0000-0000-000000000000'.
8. Loading existing IP restrictions.
9. Getting web apps for subscription '00000000-0000-0000-0000-000000000000'.
10. Getting IP restrictions for 'myweb'.
11. Found '1' existing IP restrictions.
12. Loading HTTP traffic cache.
13. Found '2' HTTP traffic cache items last synced at '3/11/2019 9:43:04 AM'.
14. Retrieving new HTTP traffic logs.
15. Connecting to FTP host 'waws-prod-bay-000.ftp.azurewebsites.windows.net'.
16. Connected. Getting remote files list.
17. Downloading remote file '/LogFiles/http/RawLogs/7cb501-201902212136.log'.
18. Deleting remote file '/LogFiles/http/RawLogs/7cb501-201902212136.log'.
19. Getting entries from HTTP log file 'D:\local\Temp\httplogs\7cb501-201902212136.log'.
20. Found '80' new HTTP log entries.
21. Updating HTTP traffic cache with '8' HTTP log entries (excludes traffic for 'xxx.xxx.xxx.xxx').
22. IP address 'xxx.xxx.xxx.xxx' count is '101'.
23. IP address 'xxx.xxx.xxx.xxx' count is '12'.
24. IP address 'xxx.xxx.xxx.xxx' count is '11'.
25. IP address 'xxx.xxx.xxx.xxx' count is '23'.
26. IP address 'xxx.xxx.xxx.xxx' count is '33'.
27. IP address 'xxx.xxx.xxx.xxx' count is '9'.
28. IP address 'xxx.xxx.xxx.xxx' count is '11'.
29. IP address 'xxx.xxx.xxx.xxx' count is '10'.
30. Saving HTTP traffic cache.
31. Removing local HTTP logs folder 'D:\local\Temp\httplogs'.
32. IP Auto Blocker function exiting. Next occurance is '3/12/2019 12:00:00 AM'.
```