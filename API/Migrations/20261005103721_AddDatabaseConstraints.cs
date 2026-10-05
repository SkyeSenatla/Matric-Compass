using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subjects_StudentId",
                table: "Subjects");

            migrationBuilder.CreateIndex(
                name: "UX_Subjects_StudentId_Code",
                table: "Subjects",
                columns: new[] { "StudentId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_BursaryApplications_StudentId_Funder_Active",
                table: "BursaryApplications",
                columns: new[] { "StudentId", "Funder" },
                unique: true,
                filter: "\"Status\" <> 4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BursaryApplications_Amount_Positive",
                table: "BursaryApplications",
                sql: "\"Amount\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Subjects_StudentId_Code",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "UX_BursaryApplications_StudentId_Funder_Active",
                table: "BursaryApplications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BursaryApplications_Amount_Positive",
                table: "BursaryApplications");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_StudentId",
                table: "Subjects",
                column: "StudentId");
        }
    }
}
