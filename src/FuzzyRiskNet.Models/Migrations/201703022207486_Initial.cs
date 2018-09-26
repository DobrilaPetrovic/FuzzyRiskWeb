namespace Nik.FuzzyRisk.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Countries",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Code = c.String(),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Criteria",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        ProjectID = c.Int(nullable: false),
                        ParentID = c.Int(),
                        Level = c.Int(nullable: false),
                        Min = c.Double(nullable: false),
                        Max = c.Double(nullable: false),
                        IndicatorID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Criteria", t => t.ParentID)
                .ForeignKey("dbo.Indicators", t => t.IndicatorID)
                .ForeignKey("dbo.Projects", t => t.ProjectID, cascadeDelete: true)
                .Index(t => t.ProjectID)
                .Index(t => t.ParentID)
                .Index(t => t.IndicatorID);
            
            CreateTable(
                "dbo.Indicators",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        DataSource = c.Int(nullable: false),
                        Code = c.String(),
                        Name = c.String(),
                        JsonDescription = c.String(),
                        JsonData = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Projects",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        UserID = c.String(maxLength: 128),
                        DivideByNumberOfDependencies = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.AspNetUsers", t => t.UserID)
                .Index(t => t.UserID);
            
            CreateTable(
                "dbo.GPNConfigurations",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        ProjectID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Projects", t => t.ProjectID, cascadeDelete: true)
                .Index(t => t.ProjectID);
            
            CreateTable(
                "dbo.Nodes",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        ProjectID = c.Int(nullable: false),
                        Name = c.String(),
                        LocationX = c.Int(nullable: false),
                        LocationY = c.Int(nullable: false),
                        DefaultPurturbation_A = c.Double(nullable: false),
                        DefaultPurturbation_B = c.Double(nullable: false),
                        DefaultPurturbation_C = c.Double(nullable: false),
                        CostPetUnitInoperability_A = c.Double(nullable: false),
                        CostPetUnitInoperability_B = c.Double(nullable: false),
                        CostPetUnitInoperability_C = c.Double(nullable: false),
                        Resilience_A = c.Double(nullable: false),
                        Resilience_B = c.Double(nullable: false),
                        Resilience_C = c.Double(nullable: false),
                        RegionID = c.Int(),
                        RoleID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Regions", t => t.RegionID)
                .ForeignKey("dbo.Criteria", t => t.RoleID)
                .ForeignKey("dbo.Projects", t => t.ProjectID, cascadeDelete: true)
                .Index(t => t.ProjectID)
                .Index(t => t.RegionID)
                .Index(t => t.RoleID);
            
            CreateTable(
                "dbo.Dependencies",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        FromID = c.Int(nullable: false),
                        ToID = c.Int(nullable: false),
                        GPNConfigurationID = c.Int(),
                        Rate_A = c.Double(nullable: false),
                        Rate_B = c.Double(nullable: false),
                        Rate_C = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.GPNConfigurations", t => t.GPNConfigurationID)
                .ForeignKey("dbo.Nodes", t => t.ToID, cascadeDelete: false)
                .ForeignKey("dbo.Nodes", t => t.FromID, cascadeDelete: false)
                .Index(t => t.FromID)
                .Index(t => t.ToID)
                .Index(t => t.GPNConfigurationID);
            
            CreateTable(
                "dbo.Regions",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        ProjectID = c.Int(nullable: false),
                        ParentID = c.Int(),
                        RegionLevel = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Regions", t => t.ParentID)
                .ForeignKey("dbo.Projects", t => t.ProjectID, cascadeDelete: true)
                .Index(t => t.ProjectID)
                .Index(t => t.ParentID);
            
            CreateTable(
                "dbo.PerturbationScenarios",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        ProjectID = c.Int(nullable: false),
                        Likelihood_A = c.Double(nullable: false),
                        Likelihood_B = c.Double(nullable: false),
                        Likelihood_C = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Projects", t => t.ProjectID, cascadeDelete: true)
                .Index(t => t.ProjectID);
            
            CreateTable(
                "dbo.PerturbationScenarioItems",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        PerturbationScenarioID = c.Int(nullable: false),
                        NodeID = c.Int(),
                        RegionID = c.Int(),
                        Purturbation_A = c.Double(nullable: false),
                        Purturbation_B = c.Double(nullable: false),
                        Purturbation_C = c.Double(nullable: false),
                        StartPeriod = c.Int(nullable: false),
                        Duration = c.Int(nullable: false),
                        RiskFactorID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Nodes", t => t.NodeID)
                .ForeignKey("dbo.Regions", t => t.RegionID)
                .ForeignKey("dbo.RiskFactors", t => t.RiskFactorID)
                .ForeignKey("dbo.PerturbationScenarios", t => t.PerturbationScenarioID, cascadeDelete: true)
                .Index(t => t.PerturbationScenarioID)
                .Index(t => t.NodeID)
                .Index(t => t.RegionID)
                .Index(t => t.RiskFactorID);
            
            CreateTable(
                "dbo.RiskFactors",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        Description = c.String(),
                        Category = c.Int(nullable: false),
                        ZoneOfInfluence = c.Int(nullable: false),
                        CompanyDefinition = c.String(),
                        CompanyHistory = c.String(),
                        MitigationMethods = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Incidents",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Description = c.String(),
                        Start = c.DateTime(nullable: false),
                        TimePeriodForDuration = c.Int(),
                        Duration_A = c.Double(nullable: false),
                        Duration_B = c.Double(nullable: false),
                        Duration_C = c.Double(nullable: false),
                        Category = c.Int(),
                        Cause = c.String(),
                        Consequences = c.String(),
                        Solution = c.String(),
                        LessonsLearned = c.String(),
                        TimePeriodTypeForLikelihood = c.Int(),
                        LikelihoodInTimePeriod_A = c.Double(nullable: false),
                        LikelihoodInTimePeriod_B = c.Double(nullable: false),
                        LikelihoodInTimePeriod_C = c.Double(nullable: false),
                        EstimatedFinancialLoss_A = c.Double(nullable: false),
                        EstimatedFinancialLoss_B = c.Double(nullable: false),
                        EstimatedFinancialLoss_C = c.Double(nullable: false),
                        OriginatedInPartnerOrRegion = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.AspNetUsers",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");
            
            CreateTable(
                "dbo.AspNetUserClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        RoleId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
            CreateTable(
                "dbo.UserSettings",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        UserID = c.String(maxLength: 128),
                        Path = c.String(),
                        Value = c.String(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.AspNetUsers", t => t.UserID)
                .Index(t => t.UserID);
            
            CreateTable(
                "dbo.CriteriaValues",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CriteriaID = c.Int(nullable: false),
                        NodeID = c.Int(nullable: false),
                        Value_A = c.Double(nullable: false),
                        Value_B = c.Double(nullable: false),
                        Value_C = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Criteria", t => t.CriteriaID, cascadeDelete: true)
                .ForeignKey("dbo.Nodes", t => t.NodeID, cascadeDelete: false)
                .Index(t => t.CriteriaID)
                .Index(t => t.NodeID);
            
            CreateTable(
                "dbo.CriteraWeights",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CriteriaID = c.Int(nullable: false),
                        GPNConfigurationID = c.Int(),
                        NodeID = c.Int(),
                        Weight = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Criteria", t => t.CriteriaID, cascadeDelete: true)
                .ForeignKey("dbo.GPNConfigurations", t => t.GPNConfigurationID)
                .ForeignKey("dbo.Nodes", t => t.NodeID)
                .Index(t => t.CriteriaID)
                .Index(t => t.GPNConfigurationID)
                .Index(t => t.NodeID);
            
            CreateTable(
                "dbo.Logs",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        UserID = c.String(),
                        Url = c.String(),
                        IP = c.String(),
                        Action = c.String(),
                        Message = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.AspNetRoles",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
            CreateTable(
                "dbo.IncidentRiskFactors",
                c => new
                    {
                        Incident_ID = c.Int(nullable: false),
                        RiskFactor_ID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Incident_ID, t.RiskFactor_ID })
                .ForeignKey("dbo.Incidents", t => t.Incident_ID, cascadeDelete: true)
                .ForeignKey("dbo.RiskFactors", t => t.RiskFactor_ID, cascadeDelete: true)
                .Index(t => t.Incident_ID)
                .Index(t => t.RiskFactor_ID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.CriteraWeights", "NodeID", "dbo.Nodes");
            DropForeignKey("dbo.CriteraWeights", "GPNConfigurationID", "dbo.GPNConfigurations");
            DropForeignKey("dbo.CriteraWeights", "CriteriaID", "dbo.Criteria");
            DropForeignKey("dbo.CriteriaValues", "NodeID", "dbo.Nodes");
            DropForeignKey("dbo.CriteriaValues", "CriteriaID", "dbo.Criteria");
            DropForeignKey("dbo.UserSettings", "UserID", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Projects", "UserID", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Regions", "ProjectID", "dbo.Projects");
            DropForeignKey("dbo.PerturbationScenarios", "ProjectID", "dbo.Projects");
            DropForeignKey("dbo.PerturbationScenarioItems", "PerturbationScenarioID", "dbo.PerturbationScenarios");
            DropForeignKey("dbo.PerturbationScenarioItems", "RiskFactorID", "dbo.RiskFactors");
            DropForeignKey("dbo.IncidentRiskFactors", "RiskFactor_ID", "dbo.RiskFactors");
            DropForeignKey("dbo.IncidentRiskFactors", "Incident_ID", "dbo.Incidents");
            DropForeignKey("dbo.PerturbationScenarioItems", "RegionID", "dbo.Regions");
            DropForeignKey("dbo.PerturbationScenarioItems", "NodeID", "dbo.Nodes");
            DropForeignKey("dbo.Nodes", "ProjectID", "dbo.Projects");
            DropForeignKey("dbo.Nodes", "RoleID", "dbo.Criteria");
            DropForeignKey("dbo.Nodes", "RegionID", "dbo.Regions");
            DropForeignKey("dbo.Regions", "ParentID", "dbo.Regions");
            DropForeignKey("dbo.Dependencies", "FromID", "dbo.Nodes");
            DropForeignKey("dbo.Dependencies", "ToID", "dbo.Nodes");
            DropForeignKey("dbo.Dependencies", "GPNConfigurationID", "dbo.GPNConfigurations");
            DropForeignKey("dbo.GPNConfigurations", "ProjectID", "dbo.Projects");
            DropForeignKey("dbo.Criteria", "ProjectID", "dbo.Projects");
            DropForeignKey("dbo.Criteria", "IndicatorID", "dbo.Indicators");
            DropForeignKey("dbo.Criteria", "ParentID", "dbo.Criteria");
            DropIndex("dbo.IncidentRiskFactors", new[] { "RiskFactor_ID" });
            DropIndex("dbo.IncidentRiskFactors", new[] { "Incident_ID" });
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropIndex("dbo.CriteraWeights", new[] { "NodeID" });
            DropIndex("dbo.CriteraWeights", new[] { "GPNConfigurationID" });
            DropIndex("dbo.CriteraWeights", new[] { "CriteriaID" });
            DropIndex("dbo.CriteriaValues", new[] { "NodeID" });
            DropIndex("dbo.CriteriaValues", new[] { "CriteriaID" });
            DropIndex("dbo.UserSettings", new[] { "UserID" });
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.PerturbationScenarioItems", new[] { "RiskFactorID" });
            DropIndex("dbo.PerturbationScenarioItems", new[] { "RegionID" });
            DropIndex("dbo.PerturbationScenarioItems", new[] { "NodeID" });
            DropIndex("dbo.PerturbationScenarioItems", new[] { "PerturbationScenarioID" });
            DropIndex("dbo.PerturbationScenarios", new[] { "ProjectID" });
            DropIndex("dbo.Regions", new[] { "ParentID" });
            DropIndex("dbo.Regions", new[] { "ProjectID" });
            DropIndex("dbo.Dependencies", new[] { "GPNConfigurationID" });
            DropIndex("dbo.Dependencies", new[] { "ToID" });
            DropIndex("dbo.Dependencies", new[] { "FromID" });
            DropIndex("dbo.Nodes", new[] { "RoleID" });
            DropIndex("dbo.Nodes", new[] { "RegionID" });
            DropIndex("dbo.Nodes", new[] { "ProjectID" });
            DropIndex("dbo.GPNConfigurations", new[] { "ProjectID" });
            DropIndex("dbo.Projects", new[] { "UserID" });
            DropIndex("dbo.Criteria", new[] { "IndicatorID" });
            DropIndex("dbo.Criteria", new[] { "ParentID" });
            DropIndex("dbo.Criteria", new[] { "ProjectID" });
            DropTable("dbo.IncidentRiskFactors");
            DropTable("dbo.AspNetRoles");
            DropTable("dbo.Logs");
            DropTable("dbo.CriteraWeights");
            DropTable("dbo.CriteriaValues");
            DropTable("dbo.UserSettings");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.Incidents");
            DropTable("dbo.RiskFactors");
            DropTable("dbo.PerturbationScenarioItems");
            DropTable("dbo.PerturbationScenarios");
            DropTable("dbo.Regions");
            DropTable("dbo.Dependencies");
            DropTable("dbo.Nodes");
            DropTable("dbo.GPNConfigurations");
            DropTable("dbo.Projects");
            DropTable("dbo.Indicators");
            DropTable("dbo.Criteria");
            DropTable("dbo.Countries");
        }
    }
}
