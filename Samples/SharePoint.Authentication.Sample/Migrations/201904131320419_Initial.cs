namespace SharePoint.Authentication.Sample.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SampleSharePointSessions",
                c => new
                    {
                        SessionId = c.Guid(nullable: false),
                        ContextToken = c.Binary(),
                        ContextTokenAuthority = c.String(),
                        SharePointHostWebUrl = c.String(),
                        SharePointAppWebUrl = c.String(),
                    })
                .PrimaryKey(t => t.SessionId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.SampleSharePointSessions");
        }
    }
}
