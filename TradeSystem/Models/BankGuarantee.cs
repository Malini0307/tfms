using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TradeSystem.Models;

namespace TradeSystem.Models
{
    public enum BgStatus
    {
        Pending,
        Issued,
        Expired
    }
    [Table("BankGuarantee")]
    public class BankGuarantee
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GuaranteeId { get; set; }

        // Link to LC
        [ForeignKey(nameof(LetterOfCredit))]
        public int LcId { get; set; }
        public LetterOfCredit? LetterOfCredit { get; set; }

        [Required, StringLength(100)]
        public string ApplicantName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string BeneficiaryName { get; set; } = string.Empty;

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal GuaranteeAmount { get; set; }

        [Required, StringLength(10)]
        public string Currency { get; set; } = "USD";

        [Required]
        public DateTime ValidityPeriod { get; set; }

        [Required]
        public BgStatus Status { get; set; } = BgStatus.Pending;



        // Navigation properties
        public ICollection<TradeDocument> TradeDocuments { get; set; }
        public ICollection<RiskAssessment> RiskAssessments { get; set; }
        public ICollection<Compliance> Compliances { get; set; }

        public BankGuarantee()
        {
            TradeDocuments = new HashSet<TradeDocument>();
            RiskAssessments = new HashSet<RiskAssessment>();
            Compliances = new HashSet<Compliance>();
        }
    }
}
