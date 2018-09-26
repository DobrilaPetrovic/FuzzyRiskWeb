using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Entity;
using System.Linq;

namespace FuzzyRiskNet.Models
{
    public class RiskDbContext : IdentityDbContext<ApplicationUser>
    {
        public RiskDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static RiskDbContext Create()
        {
            return new RiskDbContext();
        }

        public virtual DbSet<Project> Projects { get; set; }
        public virtual DbSet<Node> Nodes { get; set; }
        public virtual DbSet<Dependency> Dependencies { get; set; }

        public virtual DbSet<GPNConfiguration> GPNConfigurations { get; set; }
        public virtual DbSet<Region> Regions { get; set; }
        public virtual DbSet<PerturbationScenario> PerturbationScenarios { get; set; }
        public virtual DbSet<PerturbationScenarioItem> PerturbationScenarioItems { get; set; }

        public virtual DbSet<RiskFactor> RiskFactors { get; set; }

        public virtual DbSet<Incident> Incidents { get; set; }

        public virtual DbSet<Log> Logs { get; set; }

        public virtual DbSet<UserSetting> UserSettings { get; set; }

        public virtual DbSet<Country> Countries { get; set; }

        public virtual DbSet<Indicator> Indicators { get; set; }

        public virtual DbSet<Criteria> Criteria { get; set; }

        public virtual DbSet<CriteraWeight> CriteriaWeights { get; set; }

        public virtual DbSet<CriteriaValue> CriteriaValues { get; set; }

        public bool SuppressSaveCorrections { get; set; }

        public override int SaveChanges()
        {
            if (!SuppressSaveCorrections)
            {
                foreach (var nen in this.ChangeTracker.Entries<Project>().Where(n => n.State == EntityState.Deleted))
                {
                    foreach (var n in Nodes.Where(n2 => n2.ProjectID == nen.Entity.ID)) Nodes.Remove(n);
                    foreach (var n in Regions.Where(n => n.ProjectID == nen.Entity.ID)) Regions.Remove(n);
                    foreach (var n in PerturbationScenarios.Where(p => p.ProjectID == nen.Entity.ID)) PerturbationScenarios.Remove(n);
                    foreach (var n in Criteria.Where(p => p.ProjectID == nen.Entity.ID)) Criteria.Remove(n);
                }

                foreach (var nen in this.ChangeTracker.Entries<Criteria>().Where(n => n.State == EntityState.Deleted))
                {
                    foreach (var n in CriteriaWeights.Where(w => w.CriteriaID == nen.Entity.ID)) CriteriaWeights.Remove(n);
                    foreach (var n in CriteriaValues.Where(w => w.CriteriaID == nen.Entity.ID)) CriteriaValues.Remove(n);
                }

                foreach (var nen in this.ChangeTracker.Entries<PerturbationScenario>().Where(n => n.State == EntityState.Deleted))
                {
                    foreach (var n in PerturbationScenarioItems.Where(p => p.PerturbationScenarioID == nen.Entity.ID)) PerturbationScenarioItems.Remove(n);
                }

                foreach (var nen in this.ChangeTracker.Entries<Node>().Where(n => n.State == EntityState.Deleted))
                {
                    foreach (var d in Dependencies.Where(dep => dep.FromID == nen.Entity.ID || dep.ToID == nen.Entity.ID)) Dependencies.Remove(d);
                }

                foreach (var nen in this.ChangeTracker.Entries<GPNConfiguration>().Where(n => n.State == EntityState.Deleted))
                {
                    foreach (var d in Dependencies.Where(dep => dep.GPNConfigurationID.HasValue && dep.GPNConfigurationID == nen.Entity.ID)) Dependencies.Remove(d);
                }


            }

            return base.SaveChanges();
        }
    }
}
