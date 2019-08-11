using Microsoft.EntityFrameworkCore;

namespace Hyprsoft.Cloud.Utilities.HttpLogs.Stores
{
    public class HttpLogsDbContext : DbContext
    {
        #region Constructors

        public HttpLogsDbContext(string connectionString) : base(GetOptions(connectionString))
        {
            Database.EnsureCreated();
        }

        #endregion

        #region Properties

        public DbSet<HttpLogEntry> Entries { get; set; }

        #endregion

        #region Methods

        private static DbContextOptions GetOptions(string connectionString)
        {
            return SqlServerDbContextOptionsExtensions.UseSqlServer(new DbContextOptionsBuilder(), connectionString).Options;
        }

        #endregion
    }
}
