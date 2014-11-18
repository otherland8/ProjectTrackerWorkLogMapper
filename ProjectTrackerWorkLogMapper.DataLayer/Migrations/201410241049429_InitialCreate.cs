namespace ProjectTrackerWorkLogMapper.DataLayer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TaskCodes",
                c => new
                    {
                        TaskID = c.Int(nullable: false, identity: true),
                        JiraCode = c.String(),
                    })
                .PrimaryKey(t => t.TaskID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.TaskCodes");
        }
    }
}
