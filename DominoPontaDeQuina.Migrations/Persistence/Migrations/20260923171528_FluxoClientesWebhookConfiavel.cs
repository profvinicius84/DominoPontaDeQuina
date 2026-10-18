using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DominoPontaDeQuina.Migrations.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FluxoClientesWebhookConfiavel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PedraAberturaValorA",
                table: "RodadasPartida",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PedraAberturaValorB",
                table: "RodadasPartida",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "ParticipacoesPartida",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.CreateTable(
                name: "EventosWebhook",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartidaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JogadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersaoEstado = table.Column<long>(type: "bigint", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Destino = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ConteudoJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProximaTentativaEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmProcessamentoAteUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Tentativas = table.Column<int>(type: "int", nullable: false),
                    EntregueEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AbandonadoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimaFalha = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosWebhook", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosWebhook_Jogadores_JogadorId",
                        column: x => x.JogadorId,
                        principalTable: "Jogadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosWebhook_Partidas_PartidaId",
                        column: x => x.PartidaId,
                        principalTable: "Partidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacoesPartida_Id",
                table: "ParticipacoesPartida",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosWebhook_EntregueEmUtc_AbandonadoEmUtc_ProximaTentativaEmUtc",
                table: "EventosWebhook",
                columns: new[] { "EntregueEmUtc", "AbandonadoEmUtc", "ProximaTentativaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosWebhook_JogadorId",
                table: "EventosWebhook",
                column: "JogadorId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosWebhook_PartidaId_JogadorId_VersaoEstado",
                table: "EventosWebhook",
                columns: new[] { "PartidaId", "JogadorId", "VersaoEstado" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosWebhook");

            migrationBuilder.DropIndex(
                name: "IX_ParticipacoesPartida_Id",
                table: "ParticipacoesPartida");

            migrationBuilder.DropColumn(
                name: "PedraAberturaValorA",
                table: "RodadasPartida");

            migrationBuilder.DropColumn(
                name: "PedraAberturaValorB",
                table: "RodadasPartida");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ParticipacoesPartida");

        }
    }
}
