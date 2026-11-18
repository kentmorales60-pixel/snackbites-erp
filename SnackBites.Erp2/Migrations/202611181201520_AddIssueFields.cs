namespace SnackBites.Erp2.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIssueFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Requisitions", "IssuedById", c => c.String());
            AddColumn("dbo.Requisitions", "IssuedOn", c => c.DateTime());
            AddColumn("dbo.Requisitions", "AcknowledgedBy", c => c.String());
            AddColumn("dbo.Requisitions", "ClosedOn", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Requisitions", "ClosedOn");
            DropColumn("dbo.Requisitions", "AcknowledgedBy");
            DropColumn("dbo.Requisitions", "IssuedOn");
            DropColumn("dbo.Requisitions", "IssuedById");
        }
    }
}
