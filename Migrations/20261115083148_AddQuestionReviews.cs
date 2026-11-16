using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Syphonic.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    QuizQuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Box = table.Column<int>(type: "INTEGER", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TimesSeen = table.Column<int>(type: "INTEGER", nullable: false),
                    TimesCorrect = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAnsweredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionReviews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionReviews_QuizQuestions_QuizQuestionId",
                        column: x => x.QuizQuestionId,
                        principalTable: "QuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionReviews_QuizQuestionId",
                table: "QuestionReviews",
                column: "QuizQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionReviews_UserId_DueAtUtc",
                table: "QuestionReviews",
                columns: new[] { "UserId", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionReviews_UserId_QuizQuestionId",
                table: "QuestionReviews",
                columns: new[] { "UserId", "QuizQuestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionReviews");
        }
    }
}
