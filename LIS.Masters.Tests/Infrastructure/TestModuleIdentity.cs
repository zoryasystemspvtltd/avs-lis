using LIS.DtoModel;

namespace LIS.Masters.Tests.Infrastructure
{
    internal sealed class TestModuleIdentity : IModuleIdentity
    {
        public string ActivityMember { get; set; } = "unit-test";
        public string AccessKey { get; set; } = "DXI800";
    }
}
