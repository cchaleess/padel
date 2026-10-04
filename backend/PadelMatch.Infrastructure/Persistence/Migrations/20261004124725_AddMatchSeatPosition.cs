using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PadelMatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchSeatPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "MatchSeats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing seats all got 0: number them 0–3 per match before the unique index exists. The organizer's
            // seat takes 0 (pair A), then occupied seats before free ones, so nobody already in lands in an odd spot.
            migrationBuilder.Sql("""
                UPDATE "MatchSeats" AS s
                SET "Position" = numbered."Position"
                FROM (
                    SELECT seat."Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY seat."MatchId"
                               ORDER BY CASE WHEN seat."HolderId" = m."OrganizerId" THEN 0 ELSE 1 END,
                                        CASE seat."Status" WHEN 'Confirmed' THEN 0 WHEN 'Held' THEN 1 ELSE 2 END,
                                        seat."Id") - 1 AS "Position"
                    FROM "MatchSeats" AS seat
                    JOIN "Matches" AS m ON m."Id" = seat."MatchId"
                ) AS numbered
                WHERE s."Id" = numbered."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MatchSeats_MatchId_Position",
                table: "MatchSeats",
                columns: new[] { "MatchId", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchSeats_MatchId_Position",
                table: "MatchSeats");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "MatchSeats");
        }
    }
}
