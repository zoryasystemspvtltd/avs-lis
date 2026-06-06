using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DataAccess
{
    /// <summary>
    /// EntityFrameworkConfiguration Class , Inherits DbConfiguration
    /// </summary>
    public class EntityFrameworkConfiguration : DbConfiguration
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public EntityFrameworkConfiguration()
        {
            this.SetHistoryContext("System.Data.SqlClient",
                (connection, defaultSchema) => new ApplicationHistoryContext(connection, defaultSchema));

            AddInterceptor(new SoftDeleteInterceptor());
        }
    }
}
