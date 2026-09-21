using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompenseAgora.Data.Migrations
{
    /// <inheritdoc />
    public partial class FatorFrotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FATOR_EMISSAO_FROTA",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoFrota = table.Column<int>(type: "int", nullable: false),
                    CH4 = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    N2O = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    ParaTodos = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FATOR_EMISSAO_FROTA", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_FATOR_EMISSAO_FROTA_FROTA_CodigoFrota",
                        column: x => x.CodigoFrota,
                        principalTable: "FROTA",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FATOR_EMISSAO_FROTA_CodigoFrota",
                table: "FATOR_EMISSAO_FROTA",
                column: "CodigoFrota");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FATOR_EMISSAO_FROTA");
        }
    }
}
