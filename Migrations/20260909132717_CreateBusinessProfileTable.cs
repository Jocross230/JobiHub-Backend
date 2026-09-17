using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CVBuilder.API.Migrations
{
    /// <inheritdoc />
    public partial class CreateBusinessProfileTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Businesses",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "integer",
                        nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    UserId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    CompanyName = table.Column<string>(
                        type: "text",
                        nullable: false),

                    Industry = table.Column<string>(
                        type: "text",
                        nullable: false),

                    Description = table.Column<string>(
                        type: "text",
                        nullable: false),

                    Website = table.Column<string>(
                        type: "text",
                        nullable: false),

                    Location = table.Column<string>(
                        type: "text",
                        nullable: false),

                    CompanySize = table.Column<string>(
                        type: "text",
                        nullable: false),

                    ContactEmail = table.Column<string>(
                        type: "text",
                        nullable: false),

                    ContactPhone = table.Column<string>(
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
                        "PK_Businesses",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_Businesses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_UserId",
                table: "Businesses",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Businesses");
        }
    }
}

