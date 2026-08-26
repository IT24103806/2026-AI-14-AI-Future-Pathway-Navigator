using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathwayNavigator.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPathwayAnalysisTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PathwayAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<string>(type: "text", nullable: false),
                    RecommendationsJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathwayAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PathwayAnalyses_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PathwayAnalyses_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PathwayAnalyses_ApprovedByUserId",
                table: "PathwayAnalyses",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PathwayAnalyses_StudentProfileId",
                table: "PathwayAnalyses",
                column: "StudentProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PathwayAnalyses");
        }
    }
}
