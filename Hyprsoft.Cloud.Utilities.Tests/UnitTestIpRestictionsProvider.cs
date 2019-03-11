using Hyprsoft.Cloud.Utilities.Azure;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.Tests
{
    public class UnitTestIpRestictionsProvider : IpRestrictionsProvider
    {
        #region Methods

        protected override Task OnLoadAsync(CancellationToken cancellationToken)
        {
            if (Restrictions.Count <= 0)
            {
                Restrictions.Add(new IpRestriction { Action = "Deny", IpAddress = $"1.1.1.1{AppServiceIpRestrictionsProvider.IpAddressCidrBlockSuffix}", Name = "Block", Priority = 1 });
                Restrictions.Add(new IpRestriction { Action = "Deny", IpAddress = $"2.2.2.2{AppServiceIpRestrictionsProvider.IpAddressCidrBlockSuffix}", Name = "Block", Priority = 1 });
            }

            return Task.CompletedTask;
        }

        protected override Task OnSaveAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}
