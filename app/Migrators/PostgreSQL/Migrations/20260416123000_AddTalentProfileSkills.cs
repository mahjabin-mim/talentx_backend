using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSQL.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260416123000_AddTalentProfileSkills")]
    public partial class AddTalentProfileSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TalentProfileSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TalentProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<int>(type: "integer", nullable: false),
                    YearsOfExperience = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentProfileSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TalentProfileSkills_TalentProfiles_TalentProfileId",
                        column: x => x.TalentProfileId,
                        principalTable: "TalentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TalentProfileSkills_TalentProfileId_DisplayOrder",
                table: "TalentProfileSkills",
                columns: new[] { "TalentProfileId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TalentProfileSkills_TalentProfileId_Language",
                table: "TalentProfileSkills",
                columns: new[] { "TalentProfileId", "Language" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TalentProfileSkills");
        }
    }
}
