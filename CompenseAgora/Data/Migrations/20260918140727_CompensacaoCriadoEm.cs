using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompenseAgora.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompensacaoCriadoEm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "CriadoEm",
                table: "COMPENSACAO",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "COMPENSACAO");
        }
    }
}
