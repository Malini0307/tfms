using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradeSystem.Models
{
    [Table("RiskAssessments")]
    public class RiskAssessment
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RiskId { get; set; }

        [Required, MaxLength(30)]
        public string TransactionReference { get; set; }

        [Required, Column(TypeName = "nvarchar(max)")]
        public string RiskFactors { get; set; }      // Stored as a JSON Format

        [Required, Column(TypeName = "decimal(5,2)")]
        public decimal RiskScore { get; set; }

        [Required]
        public DateTime AssessmentDate { get; set; }

        // User tracking fields
        [Required]
        public string CreatedByUserId { get; set; }
        
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int? LcId { get; set; }
        [ForeignKey("LcId")]
        public LetterOfCredit? LetterOfCredit { get; set; }

        public int? GuaranteeId { get; set; }
        [ForeignKey("GuaranteeId")]
        public BankGuarantee? BankGuarantee { get; set; }

    }
}
