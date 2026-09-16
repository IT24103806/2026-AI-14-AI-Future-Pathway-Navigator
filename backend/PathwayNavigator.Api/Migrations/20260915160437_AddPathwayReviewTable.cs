using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathwayNavigator.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPathwayReviewTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PathwayReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PathwayAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CounsellorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsHighRisk = table.Column<bool>(type: "boolean", nullable: false),
                    RiskReason = table.Column<string>(type: "text", nullable: true),
                    MissingSkillsJson = table.Column<string>(type: "text", nullable: true),
                    FeasibilitySummary = table.Column<string>(type: "text", nullable: true),
                    CounsellorFeedback = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathwayReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PathwayReviews_PathwayAnalyses_PathwayAnalysisId",
                        column: x => x.PathwayAnalysisId,
                        principalTable: "PathwayAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PathwayReviews_Users_CounsellorId",
                        column: x => x.CounsellorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PathwayReviews_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PathwayReviews_CounsellorId",
                table: "PathwayReviews",
                column: "CounsellorId");

            migrationBuilder.CreateIndex(
                name: "IX_PathwayReviews_PathwayAnalysisId",
                table: "PathwayReviews",
                column: "PathwayAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_PathwayReviews_StudentId",
                table: "PathwayReviews",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PathwayReviews");
        }
    }
}
