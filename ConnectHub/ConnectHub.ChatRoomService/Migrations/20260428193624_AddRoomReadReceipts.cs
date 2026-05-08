using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectHub.ChatRoomService.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomReadReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: IsRead was already added in the failed attempt before it crashed on MediaUrl
            // However, since the transaction rolled back (or should have), we might need it.
            // But wait, Npgsql usually doesn't do DDL transactions unless configured.
            // In the logs, IsRead succeeded.
            
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "RoomMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "RoomMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReadAt",
                table: "RoomMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastReadMessageId",
                table: "RoomMembers",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "RoomMessages");

            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "RoomMessages");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "RoomMessages");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "RoomMessages");

            migrationBuilder.DropColumn(
                name: "LastReadAt",
                table: "RoomMembers");

            migrationBuilder.DropColumn(
                name: "LastReadMessageId",
                table: "RoomMembers");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "RoomMembers");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "RoomMessages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
