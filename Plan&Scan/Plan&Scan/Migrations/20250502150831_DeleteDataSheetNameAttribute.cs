using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plan_Scan.Migrations
{
    /// <inheritdoc />
    public partial class DeleteDataSheetNameAttribute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataSheetName",
                table: "StudentExamRegistrations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataSheetName",
                table: "StudentExamRegistrations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
