using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PadelMatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatConfirmedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmedAtUtc",
                table: "MatchSeats",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedAtUtc",
                table: "MatchSeats");
        }
    }
}
