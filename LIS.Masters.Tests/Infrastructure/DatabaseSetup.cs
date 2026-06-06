using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LIS.Masters.Tests.Infrastructure
{
    [TestClass]
    public class DatabaseSetup
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            IntegrationTestBase.DatabaseAvailable = TestServiceFactory.TryCreate(out var factory, out IntegrationTestBase.DatabaseError);
            factory?.Dispose();
        }
    }
}
