using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderScraper.Migrations
{
    /// <inheritdoc />
    public partial class AddNoticeStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NoticeStatus",
                table: "Tenders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoticeStatus",
                table: "Tenders");
        }
    }
}
