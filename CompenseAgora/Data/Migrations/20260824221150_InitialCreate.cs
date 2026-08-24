using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompenseAgora.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "COMBUSTIVEL",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UnidadeMedida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodigoCombustivelBiogenico = table.Column<int>(type: "int", nullable: true),
                    CodigoCombustivelFossil = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COMBUSTIVEL", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_COMBUSTIVEL_COMBUSTIVEL_CodigoCombustivelBiogenico",
                        column: x => x.CodigoCombustivelBiogenico,
                        principalTable: "COMBUSTIVEL",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_COMBUSTIVEL_COMBUSTIVEL_CodigoCombustivelFossil",
                        column: x => x.CodigoCombustivelFossil,
                        principalTable: "COMBUSTIVEL",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FATOR_COMPOSICAO_COMBUSTIVEL",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    PercentualEtanol = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PercentualBiodiesel = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FATOR_COMPOSICAO_COMBUSTIVEL", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "FATOR_ENERGIA",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    FeSin = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FATOR_ENERGIA", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "GAS_EFEITO_ESTUFA",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Familia = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    GWP = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GAS_EFEITO_ESTUFA", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "PESSOA",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Sobrenome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Endereco = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Pais = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Celular = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PESSOA", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "FATORES_DO_COMBUSTIVEL",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoCombustivel = table.Column<int>(type: "int", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    PoderCalorificoInferior = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Densidade = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CO2 = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CH4 = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    N2O = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FATORES_DO_COMBUSTIVEL", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_FATORES_DO_COMBUSTIVEL_COMBUSTIVEL_CodigoCombustivel",
                        column: x => x.CodigoCombustivel,
                        principalTable: "COMBUSTIVEL",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FROTA",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CodigoCombustivelPrimario = table.Column<int>(type: "int", nullable: false),
                    CodigoCombustivelBiogenico = table.Column<int>(type: "int", nullable: true),
                    CodigoCombustivelFossil = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FROTA", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_FROTA_COMBUSTIVEL_CodigoCombustivelBiogenico",
                        column: x => x.CodigoCombustivelBiogenico,
                        principalTable: "COMBUSTIVEL",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FROTA_COMBUSTIVEL_CodigoCombustivelFossil",
                        column: x => x.CodigoCombustivelFossil,
                        principalTable: "COMBUSTIVEL",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FROTA_COMBUSTIVEL_CodigoCombustivelPrimario",
                        column: x => x.CodigoCombustivelPrimario,
                        principalTable: "COMBUSTIVEL",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ENERGIA",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoPessoa = table.Column<int>(type: "int", nullable: false),
                    DataReferencia = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CriadoEm = table.Column<DateOnly>(type: "date", nullable: false),
                    EmissaoCO2 = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENERGIA", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_ENERGIA_PESSOA_CodigoPessoa",
                        column: x => x.CodigoPessoa,
                        principalTable: "PESSOA",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CONSUMO_MEDIO_FROTA",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoFrota = table.Column<int>(type: "int", nullable: false),
                    ParaTodos = table.Column<bool>(type: "bit", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    Consumo = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnidadeMedida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONSUMO_MEDIO_FROTA", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_CONSUMO_MEDIO_FROTA_FROTA_CodigoFrota",
                        column: x => x.CodigoFrota,
                        principalTable: "FROTA",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VIAGEM",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoPessoa = table.Column<int>(type: "int", nullable: false),
                    CodigoFrota = table.Column<int>(type: "int", nullable: false),
                    CodigoCombustivel = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateOnly>(type: "date", nullable: false),
                    DataReferencia = table.Column<DateOnly>(type: "date", nullable: false),
                    Consumo = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AnoFrota = table.Column<int>(type: "int", nullable: false),
                    DistanciaKM = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EmissaoCO2 = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VIAGEM", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_VIAGEM_COMBUSTIVEL_CodigoCombustivel",
                        column: x => x.CodigoCombustivel,
                        principalTable: "COMBUSTIVEL",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VIAGEM_FROTA_CodigoFrota",
                        column: x => x.CodigoFrota,
                        principalTable: "FROTA",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VIAGEM_PESSOA_CodigoPessoa",
                        column: x => x.CodigoPessoa,
                        principalTable: "PESSOA",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_COMBUSTIVEL_CodigoCombustivelBiogenico",
                table: "COMBUSTIVEL",
                column: "CodigoCombustivelBiogenico");

            migrationBuilder.CreateIndex(
                name: "IX_COMBUSTIVEL_CodigoCombustivelFossil",
                table: "COMBUSTIVEL",
                column: "CodigoCombustivelFossil");

            migrationBuilder.CreateIndex(
                name: "IX_CONSUMO_MEDIO_FROTA_CodigoFrota",
                table: "CONSUMO_MEDIO_FROTA",
                column: "CodigoFrota");

            migrationBuilder.CreateIndex(
                name: "IX_ENERGIA_CodigoPessoa",
                table: "ENERGIA",
                column: "CodigoPessoa");

            migrationBuilder.CreateIndex(
                name: "IX_FATORES_DO_COMBUSTIVEL_CodigoCombustivel",
                table: "FATORES_DO_COMBUSTIVEL",
                column: "CodigoCombustivel");

            migrationBuilder.CreateIndex(
                name: "IX_FROTA_CodigoCombustivelBiogenico",
                table: "FROTA",
                column: "CodigoCombustivelBiogenico");

            migrationBuilder.CreateIndex(
                name: "IX_FROTA_CodigoCombustivelFossil",
                table: "FROTA",
                column: "CodigoCombustivelFossil");

            migrationBuilder.CreateIndex(
                name: "IX_FROTA_CodigoCombustivelPrimario",
                table: "FROTA",
                column: "CodigoCombustivelPrimario");

            migrationBuilder.CreateIndex(
                name: "IX_VIAGEM_CodigoCombustivel",
                table: "VIAGEM",
                column: "CodigoCombustivel");

            migrationBuilder.CreateIndex(
                name: "IX_VIAGEM_CodigoFrota",
                table: "VIAGEM",
                column: "CodigoFrota");

            migrationBuilder.CreateIndex(
                name: "IX_VIAGEM_CodigoPessoa",
                table: "VIAGEM",
                column: "CodigoPessoa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CONSUMO_MEDIO_FROTA");

            migrationBuilder.DropTable(
                name: "ENERGIA");

            migrationBuilder.DropTable(
                name: "FATOR_COMPOSICAO_COMBUSTIVEL");

            migrationBuilder.DropTable(
                name: "FATOR_ENERGIA");

            migrationBuilder.DropTable(
                name: "FATORES_DO_COMBUSTIVEL");

            migrationBuilder.DropTable(
                name: "GAS_EFEITO_ESTUFA");

            migrationBuilder.DropTable(
                name: "VIAGEM");

            migrationBuilder.DropTable(
                name: "FROTA");

            migrationBuilder.DropTable(
                name: "PESSOA");

            migrationBuilder.DropTable(
                name: "COMBUSTIVEL");
        }
    }
}
