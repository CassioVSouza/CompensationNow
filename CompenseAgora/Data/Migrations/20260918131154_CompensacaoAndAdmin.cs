using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompenseAgora.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompensacaoAndAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Admin",
                table: "PESSOA",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "COMPENSACAO",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoCompensacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QuantidadeCompensada = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COMPENSACAO", x => x.Codigo);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "COMPENSACAO");

            migrationBuilder.DropColumn(
                name: "Admin",
                table: "PESSOA");
        }
    }
}
