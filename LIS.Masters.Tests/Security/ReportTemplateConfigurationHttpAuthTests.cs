using System;
using System.Linq;
using System.Reflection;
using Lis.Api.Controllers.Api;
using Lis.Api.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LIS.Masters.Tests.Security
{
    /// <summary>
    /// Phase 4: HTTP-level authorization coverage for Report Template Configuration APIs
    /// via QAuthorize attribute inspection (anonymous/non-admin must not reach unguarded actions).
    /// </summary>
    [TestClass]
    public class ReportTemplateConfigurationHttpAuthTests
    {
        [TestMethod]
        public void All_Http_Actions_Have_QAuthorize_ReportTemplateConfiguration()
        {
            var controller = typeof(ReportTemplateConfigurationController);
            var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(IsHttpAction)
                .ToList();

            Assert.IsTrue(actions.Count > 0, "Expected HTTP actions on ReportTemplateConfigurationController.");

            foreach (var action in actions)
            {
                var auth = action.GetCustomAttributes(typeof(QAuthorizeAttribute), true)
                    .Cast<QAuthorizeAttribute>()
                    .FirstOrDefault();
                Assert.IsNotNull(auth, "Missing QAuthorize on " + action.Name);
                Assert.AreEqual("ReportTemplateConfiguration", auth.ModuleName, action.Name);
            }
        }

        [TestMethod]
        public void Mutating_Actions_Require_CanAdd_Or_CanEdit()
        {
            var controller = typeof(ReportTemplateConfigurationController);

            AssertRequires(controller, "Create", ModulePermissionType.CanAdd);
            AssertRequires(controller, "DesignerCreate", ModulePermissionType.CanAdd);
            AssertRequires(controller, "SaveDraft", ModulePermissionType.CanEdit);
            AssertRequires(controller, "DesignerSave", ModulePermissionType.CanEdit);
            AssertRequires(controller, "SetMode", ModulePermissionType.CanEdit);
            AssertRequires(controller, "DesignerActivate", ModulePermissionType.CanEdit);
            AssertRequires(controller, "DesignerDeactivate", ModulePermissionType.CanEdit);
            AssertRequires(controller, "Publish", ModulePermissionType.CanEdit);
            AssertRequires(controller, "Archive", ModulePermissionType.CanEdit);
        }

        [TestMethod]
        public void Print_Modules_Do_Not_Authorize_Template_Controller()
        {
            var controller = typeof(ReportTemplateConfigurationController);
            var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(IsHttpAction);
            foreach (var action in actions)
            {
                foreach (var auth in action.GetCustomAttributes(typeof(QAuthorizeAttribute), true).Cast<QAuthorizeAttribute>())
                {
                    Assert.AreNotEqual("Reports", auth.ModuleName, action.Name);
                    Assert.AreNotEqual("RadiologyReports", auth.ModuleName, action.Name);
                }
            }
        }

        private static bool IsHttpAction(MethodInfo method)
        {
            if (method == null || method.IsSpecialName)
            {
                return false;
            }

            return method.GetCustomAttributes(true).Any(a =>
            {
                var name = a.GetType().Name;
                return name == "HttpGetAttribute"
                    || name == "HttpPostAttribute"
                    || name == "HttpPutAttribute"
                    || name == "HttpDeleteAttribute"
                    || name == "RouteAttribute";
            });
        }

        private static void AssertRequires(Type controller, string methodName, ModulePermissionType required)
        {
            var method = controller.GetMethod(methodName);
            Assert.IsNotNull(method, "Method not found: " + methodName);
            var auth = method.GetCustomAttributes(typeof(QAuthorizeAttribute), true)
                .Cast<QAuthorizeAttribute>()
                .FirstOrDefault();
            Assert.IsNotNull(auth, methodName);
            Assert.AreEqual(required, auth.ModulePermissionTypes, methodName);
        }
    }
}
