using FuzzyRiskNet.Fuzzy;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FuzzyRiskNet.Models
{
    public enum TimePeriodType { Day, Month, Year }

    /// <summary>
    /// An incident report
    /// </summary>
    public class Incident
    {
        public int ID { get; set; }

        /// <summary>
        /// The brief description of the risk incident
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The starting time of the incident
        /// </summary>
        [Required]
        public DateTime Start { get; set; }

        /// <summary>
        /// Time period used for duration.
        /// </summary>
        public TimePeriodType? TimePeriodForDuration { get; set; }

        /// <summary>
        /// The duration of the incident in the specified time period (fuzzy number)
        /// </summary>
        public TFN Duration { get; set; }

        /// <summary>
        /// Type or category of the incident
        /// </summary>
        public RiskFactorCategory? Category { get; set; }


        /// <summary>
        /// The main cause of the incident (free text)
        /// </summary>
        public string Cause { get; set; }

        /// <summary>
        /// Consequences of the incident (free text)
        /// </summary>
        public string Consequences { get; set; }

        /// <summary>
        /// The solution found for the incident (free text)
        /// </summary>
        public string Solution { get; set; }

        /// <summary>
        /// Any lessons learned from the incident (free text)
        /// </summary>
        public string LessonsLearned { get; set; }


        /// <summary>
        /// The time period used for the likelihood estimate.
        /// </summary>
        public TimePeriodType? TimePeriodTypeForLikelihood { get; set; }

        /// <summary>
        /// Estimated likelihood to happen in the defined time period.
        /// </summary>
        public TFN LikelihoodInTimePeriod { get; set; }


        /// <summary>
        /// An estimation of the total financial loss (fuzzy number)
        /// </summary>
        public TFN EstimatedFinancialLoss { get; set; }


        /// <summary>
        /// The starting point of the incident (partner or partners for actor specific incidents and region or regions for regional/external incidents) 
        /// (This is free text for now but would be good to link it directly with Region and Node classes)
        /// </summary>
        public string OriginatedInPartnerOrRegion { get; set; }

        /// <summary>
        /// A list of relevant risk factors to the incident (may need to be completed in the later phases of risk documentation)
        /// </summary>
        public virtual ICollection<RiskFactor> RiskFactors { get; set; }
    }
}
