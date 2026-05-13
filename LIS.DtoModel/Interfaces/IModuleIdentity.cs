using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel
{
    public interface IModuleIdentity
    {
        string ActivityMember { get; }

        string AccessKey { get; }
    }
}
