using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradeSystem.Models
{
    public enum LCStatus
    {
        Open = 0,
        Amended = 1,
        Closed = 2
    }

    [Table("LetterOfCredit")]
    public class LetterOfCredit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LcId { get; set; }

        [Required(ErrorMessage = "Applicant Name is required"), StringLength(100)]
        public string ApplicantName { get; set; }

        [Required(ErrorMessage = "Beneficiary Name is required"), StringLength(100)]
        public string BeneficiaryName { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required, StringLength(10)]
        public string Currency { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public LCStatus Status { get; set; } = LCStatus.Open;


        // Navigation properties
        public ICollection<BankGuarantee> BankGuarantees { get; set; }
        public ICollection<TradeDocument> TradeDocuments { get; set; }
        public ICollection<RiskAssessment> RiskAssessments { get; set; }
        public ICollection<Compliance> Compliances { get; set; }

        public LetterOfCredit()
        {
            BankGuarantees = new HashSet<BankGuarantee>();
            TradeDocuments = new HashSet<TradeDocument>();
            RiskAssessments = new HashSet<RiskAssessment>();
            Compliances = new HashSet<Compliance>();
        }
    }

}
