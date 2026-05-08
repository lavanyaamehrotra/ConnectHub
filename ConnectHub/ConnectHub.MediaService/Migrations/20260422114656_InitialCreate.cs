using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectHub.MediaService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaFiles",
                columns: table => new
                {
                    FileId = table.Column<string>(type: "text", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    FileSizeKb = table.Column<long>(type: "bigint", nullable: false),
                    BlobUrl = table.Column<string>(type: "text", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: true),
                    MessageId = table.Column<int>(type: "integer", nullable: true),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFiles", x => x.FileId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_ExpiresAt",
                table: "MediaFiles",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_MessageId",
                table: "MediaFiles",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_RoomId",
                table: "MediaFiles",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_UploadedBy",
                table: "MediaFiles",
                column: "UploadedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaFiles");
        }
    }
}
