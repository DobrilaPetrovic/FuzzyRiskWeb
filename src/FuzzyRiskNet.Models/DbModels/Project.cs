using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FuzzyRiskNet.Models
{

    /// <summary>
    /// An individual project which has its own regions, GPN configurations, Risk Scenarios and nodes.
    /// </summary>
    public class Project
    {
        public Project()
        {
            Nodes = new HashSet<Node>();
            GPNConfigurations = new HashSet<GPNConfiguration>();
            Regions = new HashSet<Region>();
            PerturbationScenarios = new HashSet<PerturbationScenario>();
            Criteria = new HashSet<Criteria>();
        }

        [JsonIgnore]
        public int ID { get; set; }

        /// <summary>
        /// Project name
        /// </summary>
        [Required]
        public string Name { get; set; }

        [JsonIgnore]
        [Required]
        public string UserID { get; set; }

        /// <summary>
        /// The owner of the project
        /// </summary>
        [ForeignKey("UserID")]
        [JsonIgnore]
        public virtual ApplicationUser User { get; set; }

        [InverseProperty("Project")]
        public virtual ICollection<Node> Nodes { get; set; }

        [InverseProperty("Project")]
        public virtual ICollection<GPNConfiguration> GPNConfigurations { get; set; }

        [InverseProperty("Project")]
        public virtual ICollection<Region> Regions { get; set; }

        [InverseProperty("Project")]
        public virtual ICollection<PerturbationScenario> PerturbationScenarios { get; set; }

        [InverseProperty("Project")]
        public virtual ICollection<Criteria> Criteria { get; set; }

        /// <summary>
        /// Divide the dependency value by the number of the nodes that the node is dependent on. This can 
        /// bring the dependency value to a managable scale.
        /// </summary>
        public bool DivideByNumberOfDependencies { get; set; }
    }
}
