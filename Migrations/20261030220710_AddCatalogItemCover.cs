using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCatalog.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogItemCover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogItemCovers",
                columns: table => new
                {
                    CatalogItemId = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ExternalItemId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ExternalImageId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ExternalWorkId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItemCovers", x => x.CatalogItemId);
                    table.ForeignKey(
                        name: "FK_CatalogItemCovers_CatalogItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogItemCovers");
        }
    }
}
