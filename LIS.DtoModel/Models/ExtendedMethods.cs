using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel.Models
{
    public static class ExtendedMethods
    {
        public static IEnumerable<T> OrderBy<T>(this System.Collections.Generic.IEnumerable<T> query, string propertyName, bool ascending)
        {

            ParameterExpression prm = Expression.Parameter(typeof(T), "it");

            Expression property = Expression.Property(prm, propertyName);

            Type propertyType = property.Type;

            MethodInfo method = typeof(ExtendedMethods).GetMethod("OrderByProperty", BindingFlags.Static | BindingFlags.NonPublic)

                .MakeGenericMethod(typeof(T), propertyType);

            return (System.Collections.Generic.IEnumerable<T>)method.Invoke(null, new object[] { query, prm, property, ascending });

        }

        private static IEnumerable<T> OrderByProperty<T, P>(this System.Collections.Generic.IEnumerable<T> query, ParameterExpression prm, Expression property, bool ascending)
        {

            Func<System.Collections.Generic.IEnumerable<T>, Func<T, P>, System.Collections.Generic.IEnumerable<T>> orderBy = (System.Collections.Generic.IEnumerable<T> q, Func<T, P> p) => ascending ? q.OrderBy(p) : q.OrderByDescending(p);

            return orderBy(query, Expression.Lambda<Func<T, P>>(property, prm).Compile());

        }
    }
}
