using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations.History;

namespace LIS.DataAccess
{
    /// <summary>
    /// Application History Context Class,Inherits HistoryContext
    /// </summary>
    public class ApplicationHistoryContext: HistoryContext
    {
        /// <summary>
        /// Parameterized Constructor
        /// </summary>
        /// <param name="dbConnection">Object Of DbConnection</param>
        /// <param name="defaultSchema">string</param>
        public ApplicationHistoryContext(DbConnection dbConnection, string defaultSchema)
            : base(dbConnection, defaultSchema)
        {
        }
        /// <summary>
        /// Creating Model
        /// </summary>
        /// <param name="modelBuilder">Object of DbModelBuilder</param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<HistoryRow>().ToTable(tableName: "MigrationHistory", schemaName: "dbo");
            modelBuilder.Entity<HistoryRow>().Property(p => p.MigrationId).HasColumnName("Migration_PK");
        }
    }
}
