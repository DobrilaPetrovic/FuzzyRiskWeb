using FuzzyRiskNet.Fuzzy;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace FuzzyRiskNet.Models
{
    public class Criteria
    {
        public Criteria()
        {
            Weights = new HashSet<CriteraWeight>();
            Values = new HashSet<CriteriaValue>();
            Childs = new HashSet<Criteria>();
        }

        [JsonIgnore]
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }

        [JsonIgnore]
        public int ProjectID { get; set; }

        /// <summary>
        /// The project that this regions belongs to
        /// </summary>
        [ForeignKey("ProjectID")]
        [JsonIgnore]
        public virtual Project Project { get; set; }
        /// <summary>
        /// A list of sub-regions within this region
        /// </summary>
        [InverseProperty("Parent")]
        [JsonIgnore]
        public virtual ICollection<Criteria> Childs { get; set; }

        /// <summary>
        /// The parent region
        /// </summary>
        [ForeignKey("ParentID")]
        public virtual Criteria Parent { get; set; }

        /// <summary>
        /// ID of the parent criteria (if any)
        /// </summary>
        [JsonIgnore]
        public int? ParentID { get; set; }

        /// <summary>
        /// Weights of criteria
        /// </summary>
        public virtual ICollection<CriteraWeight> Weights { get; set; }

        /// <summary>
        /// Values of the criteria
        /// </summary>
        public virtual ICollection<CriteriaValue> Values { get; set; }

        /// <summary>
        /// BSC Level
        /// </summary>
        public int Level { get; set; }

        public double Min { get; set; }

        public double Max { get; set; }

        [JsonIgnore]
        public int? IndicatorID { get; set; }

        /// <summary>
        /// Linked external indicator
        /// </summary>
        [ForeignKey("IndicatorID")]
        public virtual Indicator Indicator { get; set; }
    }

    public class CriteriaValue
    {
        [JsonIgnore]
        public int ID { get; set; }
        [JsonIgnore]
        public int CriteriaID { get; set; }

        [ForeignKey("CriteriaID")]
        public virtual Criteria Criteria { get; set; }

        [JsonIgnore]
        public int NodeID { get; set; }

        [ForeignKey("NodeID")]
        public virtual Node Node { get; set; }

        public virtual TFN Value { get; set; }
    }

    public class CriteraWeight
    {
        [JsonIgnore]
        public int ID { get; set; }

        [JsonIgnore]
        public int CriteriaID { get; set; }

        /// <summary>
        /// The criteria that is weighted (for all levels). 
        /// </summary>
        [ForeignKey("CriteriaID")]
        public virtual Criteria Criteria { get; set; }

        [JsonIgnore]
        public int? GPNConfigurationID { get; set; }

        /// <summary>
        /// The GPN Configuration that this weight belongs to. Only set for level 4 and 5.
        /// </summary>
        [ForeignKey("GPNConfigurationID")]
        public GPNConfiguration GPNConfiguration { get; set; }

        [JsonIgnore]
        public int? NodeID { get; set; }

        /// <summary>
        /// The node that the weight belogns to. Only set for level 4.
        /// </summary>
        [ForeignKey("NodeID")]
        public virtual Node Node { get; set; }


        public double Weight { get; set; }
    }
}
