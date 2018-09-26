namespace Nik.FuzzyRisk.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RequiredUserInProject : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Projects", "UserID", "dbo.AspNetUsers");
            DropIndex("dbo.Projects", new[] { "UserID" });
            AlterColumn("dbo.Projects", "UserID", c => c.String(nullable: false, maxLength: 128));
            CreateIndex("dbo.Projects", "UserID");
            AddForeignKey("dbo.Projects", "UserID", "dbo.AspNetUsers", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Projects", "UserID", "dbo.AspNetUsers");
            DropIndex("dbo.Projects", new[] { "UserID" });
            AlterColumn("dbo.Projects", "UserID", c => c.String(maxLength: 128));
            CreateIndex("dbo.Projects", "UserID");
            AddForeignKey("dbo.Projects", "UserID", "dbo.AspNetUsers", "Id");
        }
    }
}
