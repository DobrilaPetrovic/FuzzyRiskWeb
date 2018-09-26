using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FuzzyRiskNet.Models
{
    public enum RiskFactorCategory { Supply, Proudction, Logistics, Demand, InformationAndControl, External }

    public enum ZoneOfInfluence { Global, RegionLevel1, RegionLevel2, RegionLevel3, RegionLevel4, ActorSpecific }

    /// <summary>
    /// A 'Risk Factor'
    /// </summary>
    public class RiskFactor
    {
        [JsonIgnore]
        public int ID { get; set; }

        /// <summary>
        /// The name or title of the risk factor
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The brief description of the risk factor
        /// </summary>
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        /// <summary>
        /// Type or category of the risk factor
        /// </summary>
        [Required]
        public RiskFactorCategory Category { get; set; }


        /// <summary>
        /// Zone of influence or impact of the risk factor.
        /// </summary>
        [Required]
        public ZoneOfInfluence ZoneOfInfluence { get; set; }


        /// <summary>
        /// Customisation of the risk factor definition by the company (free text)
        /// </summary>
        [DataType(DataType.MultilineText)]
        public string CompanyDefinition { get; set; }

        /// <summary>
        /// History of the risk factor in the company (free text)
        /// </summary>
        [DataType(DataType.MultilineText)]
        public string CompanyHistory { get; set; }

        /// <summary>
        /// The mitigation methods identified by the company (free text)
        /// </summary>
        [DataType(DataType.MultilineText)]
        public string MitigationMethods { get; set; }

        /// <summary>
        /// Relevant incidents to the risk factor.
        /// </summary>
        public virtual ICollection<Incident> Incidents { get; set; }

    }
}
