using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.HttpLogs.Stores
{
    public class SqlServerHttpLogStore : HttpLogStore
    {
        #region Constructors

        public SqlServerHttpLogStore(SqlServerHttpLogStoreSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            var errors = Settings.IsValid();
            if (errors.Count() > 0)
                throw new ArgumentOutOfRangeException($"Sql Server HTTP log store settings are missing or invalid. {string.Join(" ", errors)}");
        }

        #endregion

        #region Properties

        public SqlServerHttpLogStoreSettings Settings { get; }

        #endregion

        #region Methods

        protected override async Task OnSaveEntriesAsync(IEnumerable<HttpLogEntry> entries, CancellationToken cancellationToken = default)
        {
            using (var db = new HttpLogsDbContext(Settings.ConnectionString))
            {
                using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
                {
                    db.Entries.AddRange(entries);
                    await db.SaveChangesAsync();

                    transaction.Commit();
                }   // using db transaction.
            }   // using db context.
        }

        #endregion
    }
}
