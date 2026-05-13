using LIS.DtoModel.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace LIS.DataAccess
{
    /// <summary>
    /// Application DB Context Class,Inherits DbContext
    /// </summary>
    [DbConfigurationType(typeof(EntityFrameworkConfiguration))]
    public class ApplicationDBContext : DbContext
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public ApplicationDBContext() : base("name=DefaultConnection")
        {
            Database.SetInitializer<ApplicationDBContext>(new CreateDatabaseIfNotExists<ApplicationDBContext>());
        }

        public static ApplicationDBContext Create()
        {
            return new ApplicationDBContext();
        }

        // HIS Data Used for mapping - no UI will be provided - Data will be inserted from backend
        public virtual DbSet<HisTestMaster> HisTestMaster { get; set; }
        public virtual DbSet<HISParameterMaster> HISParameterMaster { get; set; }
        public virtual DbSet<HISParameterRangMaster> HISParameterRangMaster { get; set; }
        public virtual DbSet<HISSpecimenMaster> HISSpecimenMaster { get; set; } // TODO May be not in use

        //Equipment Master and mapping with HIS
        public virtual DbSet<EquipmentMaster> EquipmentMaster { get; set; }
        public virtual DbSet<TestMappingMaster> TestMappingMaster { get; set; }
        public virtual DbSet<EquipmentHeartBeat> EquipmentHeartBeat { get; set; }
        public virtual DbSet<Departments> Department { get; set; }

        //Incoming Data
        public virtual DbSet<PatientDetail> PatientDetails { get; set; }
        public virtual DbSet<TestRequestDetail> TestRequestDetails { get; set; }
        public virtual DbSet<TestParameter> TestParameters { get; set; }

        //Outgoing Data
        public virtual DbSet<TestResult> TestResults { get; set; }
        public virtual DbSet<TestResultDetails> TestResultDetails { get; set; }

        //Control Data
        public virtual DbSet<ControlResult> ControlResults { get; set; }
        public virtual DbSet<ControlResultDetails> ControlResultDetails { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            if (modelBuilder != null)
            {
                modelBuilder.Entity<TestRequestDetail>()
                .HasIndex(entity => new { entity.SampleNo, entity.HISTestCode, entity.ReportStatus }).IsUnique();

                modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
                base.OnModelCreating(modelBuilder);
            }
        }
    }
}
