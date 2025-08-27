using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradeSystem.Models
{
    public enum TdStatus
    {
        Active,
        Archived
    }

    [Table("TradeDocuments")]
    public class TradeDocument
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DocumentId { get; set; }

        [Required, MaxLength(30)]
        public string DocumentType { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        [ValidateNever]
        public string ReferenceNumber { get; set; } = string.Empty; // Unique like ABC09876

        [Required, MaxLength(100)]
        [ValidateNever]
        public string UploadedBy { get; set; } = string.Empty;      // FullName or Email

        [Required]
        [ValidateNever]
        public DateTime UploadDate { get; set; }

        [Required]
        public TdStatus Status { get; set; }

        // User tracking fields
        [Required]
        public string CreatedByUserId { get; set; }
        
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Optional link to LC
        public int? LcId { get; set; }
        [ForeignKey(nameof(LcId))]
        public LetterOfCredit? LetterOfCredit { get; set; }

        // Optional link to BG
        public int? GuaranteeId { get; set; }
        [ForeignKey(nameof(GuaranteeId))]
        public BankGuarantee? BankGuarantee { get; set; }
    }

}
