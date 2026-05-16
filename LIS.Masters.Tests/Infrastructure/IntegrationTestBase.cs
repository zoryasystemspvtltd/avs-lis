using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LIS.Masters.Tests.Infrastructure
{
    [TestClass]
    public abstract class IntegrationTestBase
    {
        internal static bool DatabaseAvailable;
        internal static string DatabaseError;
        protected TestServiceFactory Services;

        [TestInitialize]
        public void TestInitialize()
        {
            if (!DatabaseAvailable)
            {
                Assert.Inconclusive($"Integration database unavailable: {DatabaseError}");
            }

            if (!TestServiceFactory.TryCreate(out var factory, out var error))
            {
                Assert.Inconclusive($"Cannot connect to AVSLIS: {error}");
            }

            Services = factory;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Services?.Dispose();
            Services = null;
        }

        /// <summary>Generates a unique code within master MaxLength(20); department MaxLength(15).</summary>
        protected static string UniqueCode(string prefix = "UT", int maxLength = 20)
        {
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var code = $"{prefix}{suffix}";
            return code.Length <= maxLength ? code : code.Substring(0, maxLength);
        }
    }
}
