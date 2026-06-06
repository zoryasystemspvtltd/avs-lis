using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;

namespace LIS.DataAccess
{
    /// <summary>
    /// SoftDeleteAttribute Class
    /// </summary>
    public class SoftDeleteAttribute : Attribute
    {
        #region "Constructor"
        /// <summary>
        /// Parameterized Constructor
        /// </summary>
        /// <param name="columnName">string</param>
        public SoftDeleteAttribute(string columnName)
        {
            ColumnName = columnName;
        }

        #endregion

        #region "Properties"
        /// <summary>
        /// ColumnName - string
        /// </summary>
        public string ColumnName { get; set; }

        #endregion

        #region "Methods"

        /// <summary>
        /// GetSoftDeleteColumnName
        /// </summary>
        /// <param name="type">Object of EdmType</param>
        /// <returns>string</returns>
        public static string GetSoftDeleteColumnName(EdmType type)
        {
            MetadataProperty annotation = type.MetadataProperties
                .Where(p => p.Name.EndsWith("customannotation:SoftDeleteColumnName"))
                .SingleOrDefault();

            return annotation == null ? null : (string)annotation.Value;
        }

        #endregion
    }
}