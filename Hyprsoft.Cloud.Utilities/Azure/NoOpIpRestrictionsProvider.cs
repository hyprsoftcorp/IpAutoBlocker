using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.Azure
{
    public class NoOpIpRestrictionsProvider : IpRestrictionsProvider
    {
        protected override Task OnLoadAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override Task OnSaveAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
