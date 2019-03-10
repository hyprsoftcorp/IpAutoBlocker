using Hyprsoft.Cloud.Utilities.Azure;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.Tests
{
    public class UnitTestIpRestictionsProvider : IpRestrictionsProvider
    {
        public UnitTestIpRestictionsProvider()
        {
            Restrictions = new List<IpRestriction>
            {
                new IpRestriction { Action = "Deny", IpAddress = $"1.1.1.1{AppServiceIpRestrictionsProvider.IpAddressBlockSuffix}", Name = "Block", Priority = 1 },
                new IpRestriction { Action = "Deny", IpAddress = $"2.2.2.2{AppServiceIpRestrictionsProvider.IpAddressBlockSuffix}", Name = "Block", Priority = 1 }
            };
        }

        internal List<IpRestriction> Restrictions { get; }

        protected override Task<List<IpRestriction>> OnGetRestrictionsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var items = new List<IpRestriction>(Restrictions);
            Restrictions.Clear();
            return Task.FromResult(items);
        }

        protected override Task OnAddRestrictionsAsync(IEnumerable<IpRestriction> restrictions, CancellationToken cancellationToken = default(CancellationToken))
        {
            Restrictions.AddRange(restrictions);
            return Task.CompletedTask;
        }
    }
}
