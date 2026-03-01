using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderScraper.Migrations
{
    /// <inheritdoc />
    public partial class AddNoticeIdAndVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Widen SourceId from varchar(100) to varchar(120) to accommodate versioned format
            migrationBuilder.AlterColumn<string>(
                name: "SourceId",
                table: "Tenders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            // Bare notice GUID — shared across all versions of the same tender
            migrationBuilder.AddColumn<string>(
                name: "NoticeId",
                table: "Tenders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Version string e.g. "01", "02", "07"
            migrationBuilder.AddColumn<string>(
                name: "NoticeVersion",
                table: "Tenders",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            // Index on NoticeId for fast "all versions of a tender" queries
            migrationBuilder.CreateIndex(
                name: "IX_Tenders_NoticeId",
                table: "Tenders",
                column: "NoticeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenders_NoticeId",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "NoticeId",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "NoticeVersion",
                table: "Tenders");

            migrationBuilder.AlterColumn<string>(
                name: "SourceId",
                table: "Tenders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);
        }
    }
}

