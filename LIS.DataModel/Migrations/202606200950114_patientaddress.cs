namespace LIS.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class patientaddress : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PatientDetails", "Address", c => c.String(maxLength: 500));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PatientDetails", "Address");
        }
    }
}
