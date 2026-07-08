namespace LIS.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SchemaModelSync : DbMigration
    {
        public override void Up()
        {
            // EF model snapshot sync only. Schema changes were applied by:
            // 202607081200000_SaleInvoiceDiscountFields
            // 202607081210000_SaleInvoiceDetailNullableRequestDetail
            // 202607081220000_LabResultRestructure
        }
        
        public override void Down()
        {
        }
    }
}
