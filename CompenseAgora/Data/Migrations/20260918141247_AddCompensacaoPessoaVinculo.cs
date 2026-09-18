using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompenseAgora.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompensacaoPessoaVinculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CodigoPessoa",
                table: "COMPENSACAO",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataReferencia",
                table: "COMPENSACAO",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_COMPENSACAO_CodigoPessoa",
                table: "COMPENSACAO",
                column: "CodigoPessoa");

            migrationBuilder.AddForeignKey(
                name: "FK_COMPENSACAO_PESSOA_CodigoPessoa",
                table: "COMPENSACAO",
                column: "CodigoPessoa",
                principalTable: "PESSOA",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_COMPENSACAO_PESSOA_CodigoPessoa",
                table: "COMPENSACAO");

            migrationBuilder.DropIndex(
                name: "IX_COMPENSACAO_CodigoPessoa",
                table: "COMPENSACAO");

            migrationBuilder.DropColumn(
                name: "CodigoPessoa",
                table: "COMPENSACAO");

            migrationBuilder.DropColumn(
                name: "DataReferencia",
                table: "COMPENSACAO");
        }
    }
}
