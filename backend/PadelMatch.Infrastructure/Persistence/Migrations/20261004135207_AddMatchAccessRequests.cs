using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PadelMatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchAccessRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchAccessRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchAccessRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchAccessRequests_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchAccessRequests_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchAccessVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Approve = table.Column<bool>(type: "boolean", nullable: false),
                    CastAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchAccessVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchAccessVotes_MatchAccessRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "MatchAccessRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchAccessVotes_Players_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchAccessRequests_MatchId_PlayerId",
                table: "MatchAccessRequests",
                columns: new[] { "MatchId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchAccessRequests_PlayerId",
                table: "MatchAccessRequests",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchAccessVotes_RequestId_VoterId",
                table: "MatchAccessVotes",
                columns: new[] { "RequestId", "VoterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchAccessVotes_VoterId",
                table: "MatchAccessVotes",
                column: "VoterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchAccessVotes");

            migrationBuilder.DropTable(
                name: "MatchAccessRequests");
        }
    }
}
