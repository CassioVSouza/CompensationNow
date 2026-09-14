using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompenseAgora.Data.Migrations
{
    /// <inheritdoc />
    public partial class combustivelPrincipal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CombustivelPrincipal",
                table: "COMBUSTIVEL",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CombustivelPrincipal",
                table: "COMBUSTIVEL");
        }
    }
}
