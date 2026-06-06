using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Resources;
using System.Web.Http;

namespace Lis.Api.Controllers
{
    public class ResourcesController : ApiController
    {
        // GET: api/Resources
        [AllowAnonymous]
        public IEnumerable<KeyValue> Get()
        {
            var resources = new List<KeyValue>();


            ResourceSet resourceSet = Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            foreach (DictionaryEntry entry in resourceSet)
            {
                resources.Add(
                    new Controllers.KeyValue()
                    {
                        Key = entry.Key.ToString(),
                        Value = entry.Value.ToString()
                    });
            }

            return resources;
        }
    }

    public class KeyValue
    {
        public string Key { get; set; }

        public string Value { get; set; }
    }
}
