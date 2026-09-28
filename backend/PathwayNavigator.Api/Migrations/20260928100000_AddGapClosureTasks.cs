using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PathwayNavigator.Api.Data;

#nullable disable
namespace PathwayNavigator.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260928100000_AddGapClosureTasks")]
public partial class AddGapClosureTasks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GapClosureTasks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                PathwayReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GapClosureTasks", x => x.Id);
                table.ForeignKey("FK_GapClosureTasks_PathwayReviews_PathwayReviewId", x => x.PathwayReviewId,
                    "PathwayReviews", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_GapClosureTasks_Users_StudentId", x => x.StudentId,
                    "Users", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_GapClosureTasks_PathwayReviewId", "GapClosureTasks", "PathwayReviewId");
        migrationBuilder.CreateIndex("IX_GapClosureTasks_StudentId_PathwayReviewId", "GapClosureTasks", new[] { "StudentId", "PathwayReviewId" });
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("GapClosureTasks");
}
