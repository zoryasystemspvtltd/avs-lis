using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Linq;

namespace LIS.DataAccess
{
    /// <summary>
    /// SoftDeleteQueryVisitor Classs,Inherits DefaultExpressionVisitor
    /// </summary>
    public class SoftDeleteQueryVisitor : DefaultExpressionVisitor
    {
        #region "Methods"

        /// <summary>
        /// DbExpressions
        /// </summary>
        /// <param name="expression">Object of DbScanExpression</param>
        /// <returns>DbExpression</returns>
        public override DbExpression Visit(DbScanExpression expression)
        {
            var column = SoftDeleteAttribute.GetSoftDeleteColumnName(expression.Target.ElementType);
            if (column != null)
            {
                var binding = DbExpressionBuilder.Bind(expression);
                return DbExpressionBuilder.Filter(
                    binding,
                    DbExpressionBuilder.NotEqual(
                        DbExpressionBuilder.Property(
                            DbExpressionBuilder.Variable(binding.VariableType, binding.VariableName),
                            column),
                        DbExpression.FromBoolean(true)));
            }
            else
            {
                return base.Visit(expression);
            }
        }

        #endregion
    }
}