using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DominoPontaDeQuina.Migrations.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TokensReutilizaveisAteRevogacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokensAcesso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EmitidoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevogadoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensAcesso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokensAcesso_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokensAcesso_Hash",
                table: "TokensAcesso",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokensAcesso_UsuarioId_RevogadoEmUtc",
                table: "TokensAcesso",
                columns: new[] { "UsuarioId", "RevogadoEmUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokensAcesso");
        }
    }
}
