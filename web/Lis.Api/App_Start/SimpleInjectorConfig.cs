using Lis.Api.App_Start;
using Lis.Api.Models;
using Lis.Api.Providers;
using LIS.Businesslogic;
using LIS.BusinessLogic;
using LIS.DataAccess;
using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler;
using Microsoft.Owin.Security.DataHandler.Encoder;
using Microsoft.Owin.Security.DataHandler.Serializer;
using Microsoft.Owin.Security.DataProtection;
using Owin;
using SimpleInjector;
using SimpleInjector.Diagnostics;
using SimpleInjector.Integration.WebApi;
using SimpleInjector.Lifestyles;
using System.Configuration;
using System.Web.Http;

namespace Lis.Api
{
    public class SimpleInjectorInitializer
    {
        public static Container Initialize(IAppBuilder app)
        {
            var container = GetInitializeContainer(app);

            container.Verify();

            GlobalConfiguration.Configuration.DependencyResolver =
                new SimpleInjectorWebApiDependencyResolver(container);

            return container;
        }

        public static Container GetInitializeContainer(
                  IAppBuilder app)
        {
            // Create the container as usual.
            var container = new Container();

            //var lifeStyle = Lifestyle.CreateHybrid(new AsyncScopedLifestyle(),Lifestyle.Transient);

            container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
            //container.Options.DefaultLifestyle = Lifestyle.CreateHybrid(new AsyncScopedLifestyle(), Lifestyle.Transient);

            container.RegisterInstance<IAppBuilder>(app); // TODO Singleton

            container.Register<ApplicationUserManager>(Lifestyle.Transient);

            Registration registration = container.GetRegistration(typeof(ApplicationUserManager)).Registration;

            registration.SuppressDiagnosticWarning(DiagnosticType.DisposableTransientComponent, "TODO");

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            container.Register<Models.IdentityDbContext>(Lifestyle.Singleton);
            container.Register<ApplicationDBContext>(Lifestyle.Scoped);
            container.Register<GenericUnitOfWork>(Lifestyle.Scoped);
            container.Register<AuthRepository>(Lifestyle.Transient);

            Registration registrationDb = container.GetRegistration(typeof(Models.IdentityDbContext)).Registration;

            registrationDb.SuppressDiagnosticWarning(DiagnosticType.DisposableTransientComponent, "TODO");

            container.Register<IUserStore<ApplicationUser>>(() =>
               new UserStore<ApplicationUser>(
                 container.GetInstance<Models.IdentityDbContext>()), Lifestyle.Transient);

            Registration registrationIStore = container.GetRegistration(typeof(IUserStore<ApplicationUser>)).Registration;

            registrationIStore.SuppressDiagnosticWarning(DiagnosticType.DisposableTransientComponent, "TODO");

            container.RegisterInitializer<ApplicationUserManager>(
                manager => InitializeUserManager(manager, app));

            // Setup for ISecureDataFormat
            container.Register<ISecureDataFormat<AuthenticationTicket>,
                SecureDataFormat<AuthenticationTicket>>(Lifestyle.Scoped);
            container.Register<ITextEncoder, Base64UrlTextEncoder>(Lifestyle.Scoped);
            container.Register<IDataSerializer<AuthenticationTicket>,
                TicketSerializer>(Lifestyle.Scoped);
            container.Register<IDataProtector>(
                () => new DpapiDataProtectionProvider()
                    .Create("ASP.NET Identity"),
                Lifestyle.Scoped);
            container.Register<IModuleIdentity, ModuleIdentity>(Lifestyle.Transient);
            container.Register<ILogger, Logger>(Lifestyle.Singleton);
            container.Register<IFileHandler, FileHandler>(Lifestyle.Scoped);
            container.Register<IEquipmentManager, EquipmentManager>(Lifestyle.Scoped);
            container.Register<IDepartmentManager, DepartmentManager>(Lifestyle.Scoped);
            container.Register<IEquipmentTestMappingManager, EquipmentTestMappingManager>(Lifestyle.Scoped);
            container.Register<ISpecimenManager, SpecimenManager>(Lifestyle.Scoped);
            container.Register<IHisMasterManager, HISParameterRangeMasterManager>(Lifestyle.Scoped);
            container.Register<IHisTestMasterManager, HISTestMasterManager>(Lifestyle.Scoped);
            container.Register<IPatientDetailsManager, PatientDetailManager>(Lifestyle.Scoped);
            container.Register<ITestParameterManager, TestParameterManager>(Lifestyle.Scoped);
            container.Register<ITestRequestDetailsManager, TestRequestDetailsManager>(Lifestyle.Scoped);
            container.Register<IEquipmentParamMappingManager, EquipmentParamMappingManager>(Lifestyle.Scoped);
            container.Register<IResponseManager, ResponseManager>(Lifestyle.Scoped);
            container.Register<IResultManager, ResultManager>(Lifestyle.Scoped);
            container.Register<IExternalApiManager, ExternalApiManager>(Lifestyle.Scoped);
            container.Register<IQualityControlManager, QualityControlManager>(Lifestyle.Scoped);
            container.Register<IEquipmentHeartBeatManager, EquipmentHeartBeatManager>(Lifestyle.Scoped);
            container.Register<ITestRateMasterManager, TestRateMasterManager>(Lifestyle.Scoped);
            container.Register<ISaleInvoiceManager, SaleInvoiceManager>(Lifestyle.Scoped);
            container.Register<IReportManager, ReportManager>(Lifestyle.Scoped);
            container.Register<ITestReportManager, TestReportManager>(Lifestyle.Scoped);
            container.Register<ITestResultEditManager, TestResultEditManager>(Lifestyle.Scoped);
            container.Register<IMasterCrudManager<ReferralDoctorMaster>, ReferralDoctorManager>(Lifestyle.Scoped);
            container.Register<IMasterCrudManager<CorporateMaster>, CorporateManager>(Lifestyle.Scoped);
            container.Register<IMasterCrudManager<TestGroupMaster>, TestGroupManager>(Lifestyle.Scoped);
            container.Register<IMasterCrudManager<TestCategoryMaster>, TestCategoryManager>(Lifestyle.Scoped);
            container.Register<IMasterCrudManager<UnitMaster>, UnitManager>(Lifestyle.Scoped);
            container.Register<IMasterCrudManager<MethodMaster>, MethodManager>(Lifestyle.Scoped);
            container.Register<IMasterCrudManager<SampleTypeMaster>, SampleTypeManager>(Lifestyle.Scoped);
            container.Register<IMasterCrudManager<ContainerMaster>, ContainerManager>(Lifestyle.Scoped);
            container.Register<ITestProfileMasterManager, TestProfileMasterManager>(Lifestyle.Scoped);
            container.Register<ReferralDoctorManager, ReferralDoctorManager>(Lifestyle.Scoped);
            container.Register<CorporateManager, CorporateManager>(Lifestyle.Scoped);
            container.Register<TestGroupManager, TestGroupManager>(Lifestyle.Scoped);
            container.Register<TestCategoryManager, TestCategoryManager>(Lifestyle.Scoped);
            container.Register<UnitManager, UnitManager>(Lifestyle.Scoped);
            container.Register<MethodManager, MethodManager>(Lifestyle.Scoped);
            container.Register<SampleTypeManager, SampleTypeManager>(Lifestyle.Scoped);
            container.Register<ContainerManager, ContainerManager>(Lifestyle.Scoped);
            container.Register<IMasterCrudManager<HISParameterMaster>, HisParameterMasterManager>(Lifestyle.Scoped);
            container.Register<IMasterCrudManager<HISParameterRangMaster>, HisParameterRangeCrudManager>(Lifestyle.Scoped);
            container.Register<IMasterCrudManager<TestMappingMaster>, TestMappingCrudManager>(Lifestyle.Scoped);
            container.Register<HisParameterMasterManager, HisParameterMasterManager>(Lifestyle.Scoped);
            container.Register<HisParameterRangeCrudManager, HisParameterRangeCrudManager>(Lifestyle.Scoped);
            container.Register<TestMappingCrudManager, TestMappingCrudManager>(Lifestyle.Scoped);
            container.Register<PatientMasterManager, PatientMasterManager>(Lifestyle.Scoped);
            container.Register<TestParameterCatalogManager, TestParameterCatalogManager>(Lifestyle.Scoped);
            container.Register<IHubContext<ILisClient>>(() =>
            {
                return GlobalHost.ConnectionManager.GetHubContext<LisHub, ILisClient>();
            }, Lifestyle.Scoped);
            // This is an extension method from the integration package.
            container.RegisterWebApiControllers(GlobalConfiguration.Configuration);

            container.Verify();

            return container;
        }

        private static void InitializeUserManager(
            ApplicationUserManager manager, IAppBuilder app)
        {
            manager.UserValidator =
             new UserValidator<ApplicationUser>(manager)
             {
                 AllowOnlyAlphanumericUserNames = false,
                 RequireUniqueEmail = true
             };

            //Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator()
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = false,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };

            var dataProtectionProvider =
                 app.GetDataProtectionProvider();

            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider =
                 new DataProtectorTokenProvider<ApplicationUser>(
                  dataProtectionProvider.Create("ASP.NET Identity"));
            }
        }
    }
}