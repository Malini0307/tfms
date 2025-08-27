using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradeSystem.Models
{
    public enum Status4
    {
        [Display(Name = "Compliant")] Compliant,
        [Display(Name = "Non-Compliant")] Non_Compliant
    }

    [Table("Compliances")]
    public class Compliance
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ComplianceId { get; set; }

        [Required, MaxLength(50)]
        public string TransactionReference { get; set; } = string.Empty;

        [Required]
        public Status4 ComplianceStatus { get; set; }

        [Required, MaxLength(100)]
        public string Remarks { get; set; } = "Auto";

        [Required]
        public DateTime ReportDate { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string FindingsJson { get; set; } = "{}";

        [Column(TypeName = "decimal(5,2)")]
        public decimal? OverallRiskScore { get; set; }

        // persisted PDF path relative to wwwroot (e.g., /compliance_reports/ComplianceReport_1.pdf)
        [MaxLength(300)]
        public string? PdfPath { get; set; }

        public bool IsFinalized { get; set; } = false;

        // User tracking fields
        [Required]
        public string CreatedByUserId { get; set; }
        
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Links (nullable)
        public int? LcId { get; set; }
        [ForeignKey(nameof(LcId))]
        public LetterOfCredit? LetterOfCredit { get; set; }

        public int? GuaranteeId { get; set; }
        [ForeignKey(nameof(GuaranteeId))]
        public BankGuarantee? BankGuarantee { get; set; }
    }

}
