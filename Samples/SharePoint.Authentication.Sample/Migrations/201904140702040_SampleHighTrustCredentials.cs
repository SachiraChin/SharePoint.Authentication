namespace SharePoint.Authentication.Sample.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SampleHighTrustCredentials : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SampleHighTrustCredentials",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SharePointHostWebUrlHash = c.String(maxLength: 64),
                        SharePointHostWebUrl = c.String(),
                        ClientId = c.Binary(),
                        ClientSecret = c.Binary(),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.SharePointHostWebUrlHash, unique: true, name: "IDX_SharePointHostWebUrl");
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.SampleHighTrustCredentials", "IDX_SharePointHostWebUrl");
            DropTable("dbo.SampleHighTrustCredentials");
        }
    }
}
