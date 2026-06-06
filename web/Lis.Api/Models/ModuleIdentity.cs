using LIS.DtoModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lis.Api.Models
{
    public class ModuleIdentity : IModuleIdentity
    {
        public ModuleIdentity()
        {
        }

        public string ActivityMember
        {
            get
            {
                var member = string.Empty;
                var httpContext = System.Web.HttpContext.Current;
                if (httpContext != null)
                {
                    if (httpContext.User != null)
                    {
                        member = httpContext.User.Identity.Name;
                    }
                }

                return member;
            }
        }

        public string AccessKey
        {
            get
            {
                var httpContext = System.Web.HttpContext.Current;
                if (httpContext != null)
                {
                    if (httpContext.Request != null)
                    {
                        var accessKeys = httpContext.Request.Headers.GetValues("accessKey");
                        if (accessKeys == null || accessKeys.Count() == 0)
                        {
                            throw new KeyNotFoundException("Invalid Access Key specified.");
                        }

                        var accessKey = accessKeys.First();
                        

                        return accessKey;
                    }
                }

                return string.Empty;
            }
        }
    }
}