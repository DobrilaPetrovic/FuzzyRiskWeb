using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FuzzyRiskNet.Models
{

    /// <summary>
    /// A region defined in the project
    /// </summary>
    public class Region
    {
        public Region() { Childs = new HashSet<Region>(); }

        [JsonIgnore]
        public int ID { get; set; }

        /// <summary>
        /// The name or title of the region
        /// </summary>
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
        public virtual ICollection<Region> Childs { get; set; }

        /// <summary>
        /// The parent region
        /// </summary>
        [ForeignKey("ParentID")]
        [JsonIgnore]
        public Region Parent { get; set; }

        public int? ParentID { get; set; }

        /// <summary>
        /// The level of region in the zone of influence (obviously should not get the 'ActorSpecific' value). - This should be required
        /// </summary>
        public ZoneOfInfluence? RegionLevel { get; set; }
    }
}
