using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using TradeSystem.Models;


namespace TradeSystem.Data
{
    public class TFMSDbContext : IdentityDbContext<ApplicationUser>
    {
        public TFMSDbContext(DbContextOptions<TFMSDbContext> options) : base(options) { }

        public virtual DbSet<LetterOfCredit> LetterOfCredits { get; set; }
        public virtual DbSet<BankGuarantee> BankGuarantees { get; set; }
        public virtual DbSet<TradeDocument> TradeDocuments { get; set; }
        public virtual DbSet<RiskAssessment> RiskAssessments { get; set; }
        public virtual DbSet<Compliance> Compliances { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<LetterOfCredit>()
                .Property(l => l.Status)
                .HasConversion<string>();

            builder.Entity<BankGuarantee>()
                   .Property(b => b.Status)
                   .HasConversion<string>();

            // Store TdStatus as string
            //builder.Entity<TradeDocument>()
            //       .Property(t => t.Status)
            //       .HasConversion<string>();

            // LetterOfCredit → TradeDocuments (No Cascade)
            builder.Entity<LetterOfCredit>()
                .HasMany(lc => lc.TradeDocuments)
                .WithOne(td => td.LetterOfCredit)       // or. WithMany() if you don't have collection
                .HasForeignKey(td => td.LcId)
                .OnDelete(DeleteBehavior.Restrict); // or NoAction

            // BankGuarantee → TradeDocuments (No Cascade)
            builder.Entity<BankGuarantee>()
                .HasMany(bg => bg.TradeDocuments)
                .WithOne(td => td.BankGuarantee)
                .HasForeignKey(td => td.GuaranteeId)
                .OnDelete(DeleteBehavior.Restrict);

            // LetterOfCredit → BankGuarantees (No Cascade)
            builder.Entity<LetterOfCredit>()
                .HasMany(lc => lc.BankGuarantees)
                .WithOne(bg => bg.LetterOfCredit)
                .HasForeignKey(bg => bg.LcId)
                .OnDelete(DeleteBehavior.Restrict);

            // Optional: unique reference number in TradeDocument
            builder.Entity<TradeDocument>()
                .HasIndex(td => td.ReferenceNumber)
                .IsUnique();

            builder.Entity<Compliance>()
            .Property(c => c.ComplianceStatus)
            .HasConversion<string>();

            builder.Entity<Compliance>()
                .HasOne(c => c.LetterOfCredit)
                .WithMany(l => l.Compliances)
                .HasForeignKey(c => c.LcId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Compliance>()
                .Property(x => x.PdfPath)
                .HasMaxLength(300);

        }

    }
}
