using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompenseAgora.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AUDITORIA",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoPessoa = table.Column<int>(type: "int", nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Entidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CodigoRegistro = table.Column<int>(type: "int", nullable: true),
                    Tela = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DataInicioFiltro = table.Column<DateOnly>(type: "date", nullable: true),
                    DataFimFiltro = table.Column<DateOnly>(type: "date", nullable: true),
                    Detalhes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDITORIA", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_AUDITORIA_PESSOA_CodigoPessoa",
                        column: x => x.CodigoPessoa,
                        principalTable: "PESSOA",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AUDITORIA_CodigoPessoa_DataHora",
                table: "AUDITORIA",
                columns: new[] { "CodigoPessoa", "DataHora" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AUDITORIA");
        }
    }
}
