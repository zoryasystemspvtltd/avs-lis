namespace Lis.Api.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IdentityModelSync : DbMigration
    {
        public override void Up()
        {
            // EF model snapshot sync only. Doctor columns were applied by
            // 202606281200000_DoctorDesignationSignature.
        }
        
        public override void Down()
        {
        }
    }
}
