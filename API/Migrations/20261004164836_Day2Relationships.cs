using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class Day2Relationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareerRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AptitudeTestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerName = table.Column<string>(type: "text", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerRecommendations_AptitudeTests_AptitudeTestId",
                        column: x => x.AptitudeTestId,
                        principalTable: "AptitudeTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Guardians",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    ContactNumber = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guardians", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BursaryApplications_StudentId",
                table: "BursaryApplications",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerRecommendations_AptitudeTestId",
                table: "CareerRecommendations",
                column: "AptitudeTestId");

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_StudentId",
                table: "Guardians",
                column: "StudentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BursaryApplications_Students_StudentId",
                table: "BursaryApplications",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BursaryApplications_Students_StudentId",
                table: "BursaryApplications");

            migrationBuilder.DropTable(
                name: "CareerRecommendations");

            migrationBuilder.DropTable(
                name: "Guardians");

            migrationBuilder.DropIndex(
                name: "IX_BursaryApplications_StudentId",
                table: "BursaryApplications");
        }
    }
}
