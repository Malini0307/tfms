using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCompliancePdfAndFinalize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compliances_LetterOfCredit_LcId",
                table: "Compliances");

            migrationBuilder.AlterColumn<string>(
                name: "PdfPath",
                table: "Compliances",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinalized",
                table: "Compliances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Compliances_LetterOfCredit_LcId",
                table: "Compliances",
                column: "LcId",
                principalTable: "LetterOfCredit",
                principalColumn: "LcId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compliances_LetterOfCredit_LcId",
                table: "Compliances");

            migrationBuilder.DropColumn(
                name: "IsFinalized",
                table: "Compliances");

            migrationBuilder.AlterColumn<string>(
                name: "PdfPath",
                table: "Compliances",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Compliances_LetterOfCredit_LcId",
                table: "Compliances",
                column: "LcId",
                principalTable: "LetterOfCredit",
                principalColumn: "LcId");
        }
    }
}
