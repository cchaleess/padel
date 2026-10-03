using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PadelMatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchSeats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchSeats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    HeldUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchSeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchSeats_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchSeats_Players_HolderId",
                        column: x => x.HolderId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchSeats_HolderId",
                table: "MatchSeats",
                column: "HolderId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchSeats_MatchId",
                table: "MatchSeats",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchSeats_MatchId_HolderId",
                table: "MatchSeats",
                columns: new[] { "MatchId", "HolderId" },
                unique: true,
                filter: "\"HolderId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchSeats");
        }
    }
}
