using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddTalentProfileForeignKeyToApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"Applications\";");

            migrationBuilder.DropColumn(
                name: "TalentId",
                table: "Applications");

            migrationBuilder.AddColumn<Guid>(
                name: "TalentId",
                table: "Applications",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_TalentId",
                table: "Applications",
                column: "TalentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_TalentProfiles_TalentId",
                table: "Applications",
                column: "TalentId",
                principalTable: "TalentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_TalentProfiles_TalentId",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_TalentId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TalentId",
                table: "Applications");

            migrationBuilder.AddColumn<int>(
                name: "TalentId",
                table: "Applications",
                type: "integer",
                nullable: true);
        }
    }
}
