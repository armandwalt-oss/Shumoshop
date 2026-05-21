using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShippingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UseLiveTcgRates = table.Column<bool>(type: "bit", nullable: false),
                    DoorToDoorRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DoorToLockerRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DoorToKioskRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FreeShippingThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarkupRand = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShippingSettings");
        }
    }
}
