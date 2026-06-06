using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.ModelBinding;

namespace Lis.Api.Models
{
    public static class ErrorResults
    {
        public static IList<KeyValuePair<string,string>> ToKeyValuePair(this ModelStateDictionary modelState)
        {
            var errors = new List<KeyValuePair<string,string>>();
            var errorVals = modelState.Values.ToArray();
            var errorKeys = modelState.Keys.ToArray();

            for (int i=0;i< errorKeys.Length;i++)
            {
                var val = errorVals[i];
                var maxKey = errorKeys[i].Split('.');
                var key = maxKey.Length > 1 ? maxKey[1] : maxKey[0];

                foreach (var error in val.Errors)
                {
                    if (!string.IsNullOrEmpty(error.ErrorMessage))
                    {
                        errors.Add(new KeyValuePair<string, string>(key, error.ErrorMessage));
                    }
                    else
                    {
                        if (error.Exception != null && !string.IsNullOrEmpty(error.Exception.Message))
                        {
                            errors.Add(new KeyValuePair<string, string>(key, error.Exception.Message));
                        }
                    }
                }

            }
            return errors;
        }


    }

    
}