using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectHub.ChatRoomService.Migrations
{
    /// <inheritdoc />
    public partial class FixMissingIsRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "RoomMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "RoomMessages");
        }
    }
}
