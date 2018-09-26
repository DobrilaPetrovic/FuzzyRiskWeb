using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using FuzzyRiskNet.Fuzzy;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace FuzzyRiskNet.Models
{
    public enum Role { Supplier, ProductionFacility, Logistics, Warehouse, Customer, ConsumerMarket }
    public class Node
    {
        public Node() { DefaultPurturbation = new TFN(); CostPetUnitInoperability = new TFN(); Resilience = new TFN() { A = 1, B = 1, C = 1 }; Dependencies = new HashSet<Dependency>(); }
 
        [JsonIgnore]       
        public int ID { get; set; }

        [JsonIgnore]
        public int ProjectID { get; set; }

        [ForeignKey("ProjectID")]
        public virtual Project Project { get; set; }

        /// <summary>
        /// Name or title of the node (company name, etc.)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The title used for the node that includes the region.
        /// </summary>
        [NotMapped]
        public string FullName { get { return Region == null ? Name : (Name + " (" + Region.Name + ")"); } }


        /// <summary>
        /// X-Location on the GPN drawing
        /// </summary>
        public int LocationX { get; set; }

        /// Y-Location on the GPN drawing
        public int LocationY { get; set; }
        
        /// <summary>
        /// The default perturbation value of the node (used for the 'ad-hoc' analysis)
        /// </summary>
        [Display(Name = "Perturbation")]
        public virtual TFN DefaultPurturbation { get; set; }

        /// <summary>
        /// The intended revenue of the node
        /// </summary>
        [Display(Name="Intended Revenue")]
        public virtual TFN CostPetUnitInoperability { get; set; }

        /// <summary>
        /// Resilience factor of the node (a fuzzy number between zero and one)
        /// </summary>
        public virtual TFN Resilience { get; set; }

        /// <summary>
        /// A list of all dependencies (that the node is dependent on)
        /// </summary>
        [InverseProperty("From")]
        public virtual ICollection<Dependency> Dependencies { get; set; }

        [JsonIgnore]
        public int? RegionID { get; set; }

        /// <summary>
        /// The region of the node
        /// </summary>
        [ForeignKey("RegionID")]
        public virtual Region Region { get; set; }

        [JsonIgnore]
        public int? RoleID { get; set; }

        [ForeignKey("RoleID")]
        public virtual Criteria Role { get; set; }
    }

    public class GPNConfiguration
    {
        public GPNConfiguration() { }

        [JsonIgnore]
        public int ID { get; set; }

        /// <summary>
        /// Name or title of the GPN configuration
        /// </summary>
        public string Name { get; set; }

        [JsonIgnore]
        public int ProjectID { get; set; }

        /// <summary>
        /// The project that the GPN configuration belongs to
        /// </summary>
        [ForeignKey("ProjectID")]
        public virtual Project Project { get; set; }
    }

    /// <summary>
    /// A dependency between two nodes in a GPN Configuration
    /// </summary>
    public class Dependency
    {
        public Dependency() { Rate = new TFN(); }
        public Dependency(Node From, Node To, TFN Rate) { this.From = From; this.To = To; this.Rate = Rate; }

        [JsonIgnore]
        public int ID { get; set; }

        /// <summary>
        /// ID of the dependent node
        /// </summary>
        [JsonIgnore]
        public int FromID { get; set; }

        /// <summary>
        /// ID of the supporting node
        /// </summary>
        [JsonIgnore]
        public int ToID { get; set; }

        /// <summary>
        /// A reference to the dependent node
        /// </summary>
        [ForeignKey("FromID")]
        public virtual Node From { get; set; }
        /// <summary>
        /// A reference to the supporting node
        /// </summary>
        [ForeignKey("ToID")]
        public virtual Node To { get; set; }

        [JsonIgnore]
        public int? GPNConfigurationID { get; set; }

        /// <summary>
        /// The GPN Configuration that this dependency is defined for - or null for default configuration.
        /// </summary>
        [ForeignKey("GPNConfigurationID")]
        public GPNConfiguration GPNConfiguration { get; set; }

        /// <summary>
        /// Overall rate of dependency
        /// </summary>
        public virtual TFN Rate { get; set; }
    }
}
