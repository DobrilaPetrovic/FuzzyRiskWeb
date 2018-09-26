using Newtonsoft.Json;
using FuzzyRiskNet.Fuzzy;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FuzzyRiskNet.Models
{
    /// <summary>
    /// A 'Risk Scenario'
    /// </summary>
    public class PerturbationScenario
    {
        public PerturbationScenario() { Items = new HashSet<PerturbationScenarioItem>(); }
        [JsonIgnore]
        public int ID { get; set; }


        /// <summary>
        /// The name or title of the risk scenario
        /// </summary>
        [Required]
        public string Name { get; set; }

        [JsonIgnore]
        public int ProjectID { get; set; }

        /// <summary>
        /// The project that this risk scenario belongs to
        /// </summary>
        [ForeignKey("ProjectID")]
        public virtual Project Project { get; set; }

        /// <summary>
        /// A list of all perturbations
        /// </summary>
        [InverseProperty("PerturbationScenario")]
        public virtual ICollection<PerturbationScenarioItem> Items { get; set; }

        /// <summary>
        /// The likelihood of the scenario (high likely is it to happen)
        /// </summary>
        [Required]
        public virtual TFN Likelihood { get; set; }
    }

    /// <summary>
    /// A particular perturbation to a risk scenario
    /// </summary>
    public class PerturbationScenarioItem
    {

        [JsonIgnore]
        public int ID { get; set; }

        [JsonIgnore]
        public int PerturbationScenarioID { get; set; }

        /// <summary>
        /// A reference to the risk scenario that this perturbation belongs to
        /// </summary>
        [ForeignKey("PerturbationScenarioID")]
        public virtual PerturbationScenario PerturbationScenario { get; set; }

        [JsonIgnore]
        public int? NodeID { get; set; }

        /// <summary>
        /// The node that is impacted by the perturbation
        /// </summary>
        [ForeignKey("NodeID")]
        public virtual Node Node { get; set; }

        [JsonIgnore]
        public int? RegionID { get; set; }

        /// <summary>
        /// The region that is impacted by the perturbation
        /// </summary>
        [ForeignKey("RegionID")]
        public virtual Region Region { get; set; }

        /// <summary>
        /// The impact of the perturbation
        /// </summary>
        [Display(Name = "Impact")]
        [Required]
        public virtual TFN Purturbation { get; set; }

        /// <summary>
        /// The starting time period of perturbation
        /// </summary>
        [Required]
        public int StartPeriod { get; set; }

        /// <summary>
        /// The duration (length) of the perturbation
        /// </summary>
        [Required]
        public int Duration { get; set; }

        [JsonIgnore]
        public int? RiskFactorID { get; set; }

        /// <summary>
        /// The risk factor that is causing the perturbation - This should be required.
        /// </summary>
        [ForeignKey("RiskFactorID")]
        public virtual RiskFactor RiskFactor { get; set; }
    }
}
