using System.ComponentModel.DataAnnotations;

namespace FuzzyRiskNet.Models
{
    public class Country
    {
        public int ID { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }
    }

    public enum DataSources { WorldBank }

    public class Indicator
    {
        [Key]
        public int ID { get; set; }

        public DataSources DataSource { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        [MaxLength]
        public string JsonDescription { get; set; }

        [MaxLength]
        public string JsonData { get; set; }
    }
}
