namespace GraphExplorerMVC.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AssetAdded : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Applications",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Assets",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Description = c.String(),
                        RawUrl = c.String(),
                        Published = c.String(),
                        Application_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Applications", t => t.Application_Id)
                .Index(t => t.Application_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Assets", "Application_Id", "dbo.Applications");
            DropIndex("dbo.Assets", new[] { "Application_Id" });
            DropTable("dbo.Assets");
            DropTable("dbo.Applications");
        }
    }
}
