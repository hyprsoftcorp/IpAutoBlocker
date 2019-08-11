using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public bool IsInitialized { get; private set; }

        public List<IpRestriction> Restrictions { get; } = new List<IpRestriction>();

        internal ILogger Logger { get; set; }

        #endregion

        #region Methods

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (IsInitialized)
                throw new InvalidOperationException("This provider is already initialized.");

            Logger.LogInformation($"Initializing provider '{GetType().Name}'.");

            return OnInitializeAsync(cancellationToken);
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            if (!IsInitialized)
                throw new InvalidOperationException($"This provider has not been initialized.  Please call '{nameof(InitializeAsync)}()'.");

            Logger.LogInformation("Loading existing IP restrictions.");

            return OnLoadAsync(cancellationToken);
        }

        public Task SaveAsync(CancellationToken cancellationToken = default)
        {
            if (!IsInitialized)
                throw new InvalidOperationException($"This provider has not been initialized.  Please call '{nameof(InitializeAsync)}()'.");

            // Make sure we always have our default allow all rule.
            var allowAllRule = Restrictions.FirstOrDefault(x => x.IpAddress == "0.0.0.0/0" && x.Action == "Allow");
            if (allowAllRule == null)
            {
                Logger.LogInformation("Adding new IP restriction for '0.0.0.0/0' with action 'Allow'.");
                Restrictions.Add(new IpRestriction
                {
                    IsNew = true,
                    IpAddress = $"0.0.0.0/0",
                    Action = "Allow",
                    Priority = 100,
                    Name = "Allow"
                });
            }
            Logger.LogInformation("Saving new IP restrictions.");

            return OnSaveAsync(cancellationToken);
        }

        protected virtual Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            IsInitialized = true;

            return Task.CompletedTask;
        }

        protected abstract Task OnLoadAsync(CancellationToken cancellationToken);

        protected abstract Task OnSaveAsync(CancellationToken cancellationToken);

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
