using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CVBuilder.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "integer",
                        nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    BusinessId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    Title = table.Column<string>(
                        type: "text",
                        nullable: false),

                    Description = table.Column<string>(
                        type: "text",
                        nullable: false),

                    Responsibilities = table.Column<string>(
                        type: "text",
                        nullable: false),

                    Requirements = table.Column<string>(
                        type: "text",
                        nullable: false),

                    Skills = table.Column<string>(
                        type: "text",
                        nullable: false),

                    Location = table.Column<string>(
                        type: "text",
                        nullable: false),

                    WorkArrangement = table.Column<string>(
                        type: "text",
                        nullable: false),

                    EmploymentType = table.Column<string>(
                        type: "text",
                        nullable: false),

                    ExperienceLevel = table.Column<string>(
                        type: "text",
                        nullable: false),

                    Salary = table.Column<string>(
                        type: "text",
                        nullable: false),

                    ApplicationMethod = table.Column<string>(
                        type: "text",
                        nullable: false),

                    ClosingDate = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true),

                    Status = table.Column<string>(
                        type: "text",
                        nullable: false),

                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),

                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Jobs",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_Jobs_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_BusinessId",
                table: "Jobs",
                column: "BusinessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}