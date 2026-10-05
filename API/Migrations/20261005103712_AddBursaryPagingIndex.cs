using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddBursaryPagingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BursaryApplications_StudentId",
                table: "BursaryApplications");

            migrationBuilder.CreateIndex(
                name: "IX_BursaryApplications_StudentId_Deadline_Id",
                table: "BursaryApplications",
                columns: new[] { "StudentId", "Deadline", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BursaryApplications_StudentId_Deadline_Id",
                table: "BursaryApplications");

            migrationBuilder.CreateIndex(
                name: "IX_BursaryApplications_StudentId",
                table: "BursaryApplications",
                column: "StudentId");
        }
    }
}
