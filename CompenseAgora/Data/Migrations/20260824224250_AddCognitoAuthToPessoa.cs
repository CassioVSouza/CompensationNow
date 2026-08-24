using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompenseAgora.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCognitoAuthToPessoa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CognitoSub",
                table: "PESSOA",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PESSOA_CognitoSub",
                table: "PESSOA",
                column: "CognitoSub",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PESSOA_Email",
                table: "PESSOA",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PESSOA_CognitoSub",
                table: "PESSOA");

            migrationBuilder.DropIndex(
                name: "IX_PESSOA_Email",
                table: "PESSOA");

            migrationBuilder.DropColumn(
                name: "CognitoSub",
                table: "PESSOA");
        }
    }
}
