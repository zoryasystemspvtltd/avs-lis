using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.BusinessLogic
{
    public static class ObjectExtensionMethods
    {
        public static IQueryable<T> ToQueryable<T>(this T instance)
        {
            return new[] { instance }.AsQueryable();
        }

        /// <summary>
        /// If the age of a person is 19 year , 4 month
        /// this will return as 19.04
        /// </summary>
        /// <param name="dob"></param>
        /// <returns></returns>
        public static decimal Age(this DateTime dob)
        {
            DateTime now = DateTime.Today;
            int ageInMonth = (now.Year - dob.Year) * 12 + (now.Month - dob.Month);

            int ageYear = ageInMonth / 12;
            decimal ageMonth = ((decimal)(ageInMonth % 12)) / 100;

            decimal age = (decimal)ageYear + ageMonth;
            return age;
        }
    }
}
