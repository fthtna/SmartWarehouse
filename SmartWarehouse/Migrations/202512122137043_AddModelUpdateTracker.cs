namespace SmartWarehouse.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddModelUpdateTracker : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ModelUpdateTrackers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LastUpdateDate = c.DateTime(nullable: false),
                        SalesSinceLastUpdate = c.Int(nullable: false),
                        ModelVersion = c.String(),
                        TotalSalesUsed = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.ModelUpdateTrackers");
        }
    }
}
