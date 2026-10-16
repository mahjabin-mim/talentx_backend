using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddJobForeignKeyToApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"Applications\";");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Applications");

            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "Applications",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_JobId",
                table: "Applications",
                column: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Jobs_JobId",
                table: "Applications",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Jobs_JobId",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_JobId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Applications");

            migrationBuilder.AddColumn<int>(
                name: "JobId",
                table: "Applications",
                type: "integer",
                nullable: true);
        }
    }
}
