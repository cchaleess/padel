using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PadelMatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessRequestPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestedPosition",
                table: "MatchAccessRequests",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedPosition",
                table: "MatchAccessRequests");
        }
    }
}
