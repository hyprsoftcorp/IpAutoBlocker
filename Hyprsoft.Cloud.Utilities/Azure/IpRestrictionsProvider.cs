using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.Azure
{
    public abstract class IpRestrictionsProvider : IDisposable
    {
        #region Fields

        private bool _isDisposed;

        #endregion

        #region Properties

        internal ILogger Logger { get; set; }

        #endregion

        #region Methods

        public Task<List<IpRestriction>> GetRestrictionsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogInformation("Retrieving existing IP restrictions.");
            return OnGetRestrictionsAsync(cancellationToken);
        }

        public Task AddRestrictionsAsync(IEnumerable<IpRestriction> restrictions, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogInformation("Submitting new IP restrictions.");
            return OnAddRestrictionsAsync(restrictions, cancellationToken);
        }

        protected abstract Task<List<IpRestriction>> OnGetRestrictionsAsync(CancellationToken cancellationToken = default(CancellationToken));

        protected abstract Task OnAddRestrictionsAsync(IEnumerable<IpRestriction> restrictions, CancellationToken cancellationToken = default(CancellationToken));


        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            // Managed resources.
            if (disposing) { }

            // Unmanaged resources.

            _isDisposed = true;
        }

        #endregion
    }
}
