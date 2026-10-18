using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DominoPontaDeQuina.Migrations.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EstadoPartidaRecuperavel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EncerradaEmUtc",
                table: "Partidas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VencedorJogadorId",
                table: "Partidas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VersaoEstado",
                table: "Partidas",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Posicao",
                table: "ParticipacoesPartida",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuantidadePedrasNaMao",
                table: "ParticipacoesPartida",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // A base anterior não possuía posição. A numeração determinística
            // permite criar a restrição única mesmo quando já houver duas
            // participações históricas na mesma partida.
            migrationBuilder.Sql("""
                WITH ParticipacoesOrdenadas AS
                (
                    SELECT PartidaId, JogadorId,
                        ROW_NUMBER() OVER (PARTITION BY PartidaId ORDER BY JogadorId) AS NovaPosicao
                    FROM ParticipacoesPartida
                )
                UPDATE participacao
                SET Posicao = ordenada.NovaPosicao
                FROM ParticipacoesPartida AS participacao
                INNER JOIN ParticipacoesOrdenadas AS ordenada
                    ON ordenada.PartidaId = participacao.PartidaId
                    AND ordenada.JogadorId = participacao.JogadorId;
                """);

            migrationBuilder.CreateTable(
                name: "PedrasMaoPartida",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartidaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JogadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValorA = table.Column<int>(type: "int", nullable: false),
                    ValorB = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedrasMaoPartida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedrasMaoPartida_ParticipacoesPartida_PartidaId_JogadorId",
                        columns: x => new { x.PartidaId, x.JogadorId },
                        principalTable: "ParticipacoesPartida",
                        principalColumns: new[] { "PartidaId", "JogadorId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RodadasPartida",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartidaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    JogadorDoTurnoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuantidadeNaCava = table.Column<int>(type: "int", nullable: false),
                    PontaEsquerda = table.Column<int>(type: "int", nullable: true),
                    PontaDireita = table.Column<int>(type: "int", nullable: true),
                    VencedorJogadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MotivoEncerramento = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IniciadaEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EncerradaEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RodadasPartida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RodadasPartida_Partidas_PartidaId",
                        column: x => x.PartidaId,
                        principalTable: "Partidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcoesPartida",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartidaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RodadaPartidaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AutorJogadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ChaveIdempotencia = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersaoEsperada = table.Column<long>(type: "bigint", nullable: false),
                    VersaoResultante = table.Column<long>(type: "bigint", nullable: true),
                    RecebidaEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrdemRecebimento = table.Column<long>(type: "bigint", nullable: false),
                    FoiAceita = table.Column<bool>(type: "bit", nullable: false),
                    ValorA = table.Column<int>(type: "int", nullable: true),
                    ValorB = table.Column<int>(type: "int", nullable: true),
                    Lado = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    PontosConcedidos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcoesPartida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcoesPartida_Jogadores_AutorJogadorId",
                        column: x => x.AutorJogadorId,
                        principalTable: "Jogadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcoesPartida_Partidas_PartidaId",
                        column: x => x.PartidaId,
                        principalTable: "Partidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcoesPartida_RodadasPartida_RodadaPartidaId",
                        column: x => x.RodadaPartidaId,
                        principalTable: "RodadasPartida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedrasCavaPartida",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RodadaPartidaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    ValorA = table.Column<int>(type: "int", nullable: false),
                    ValorB = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedrasCavaPartida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedrasCavaPartida_RodadasPartida_RodadaPartidaId",
                        column: x => x.RodadaPartidaId,
                        principalTable: "RodadasPartida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Partidas_Id_VersaoEstado",
                table: "Partidas",
                columns: new[] { "Id", "VersaoEstado" });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacoesPartida_PartidaId_Posicao",
                table: "ParticipacoesPartida",
                columns: new[] { "PartidaId", "Posicao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcoesPartida_AutorJogadorId",
                table: "AcoesPartida",
                column: "AutorJogadorId");

            migrationBuilder.CreateIndex(
                name: "IX_AcoesPartida_PartidaId_AutorJogadorId_ChaveIdempotencia",
                table: "AcoesPartida",
                columns: new[] { "PartidaId", "AutorJogadorId", "ChaveIdempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcoesPartida_PartidaId_OrdemRecebimento",
                table: "AcoesPartida",
                columns: new[] { "PartidaId", "OrdemRecebimento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcoesPartida_PartidaId_RecebidaEmUtc_OrdemRecebimento",
                table: "AcoesPartida",
                columns: new[] { "PartidaId", "RecebidaEmUtc", "OrdemRecebimento" });

            migrationBuilder.CreateIndex(
                name: "IX_AcoesPartida_RodadaPartidaId",
                table: "AcoesPartida",
                column: "RodadaPartidaId");

            migrationBuilder.CreateIndex(
                name: "IX_PedrasCavaPartida_RodadaPartidaId_Ordem",
                table: "PedrasCavaPartida",
                columns: new[] { "RodadaPartidaId", "Ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedrasMaoPartida_PartidaId_JogadorId_ValorA_ValorB",
                table: "PedrasMaoPartida",
                columns: new[] { "PartidaId", "JogadorId", "ValorA", "ValorB" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RodadasPartida_PartidaId_Numero",
                table: "RodadasPartida",
                columns: new[] { "PartidaId", "Numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcoesPartida");

            migrationBuilder.DropTable(
                name: "PedrasCavaPartida");

            migrationBuilder.DropTable(
                name: "PedrasMaoPartida");

            migrationBuilder.DropTable(
                name: "RodadasPartida");

            migrationBuilder.DropIndex(
                name: "IX_Partidas_Id_VersaoEstado",
                table: "Partidas");

            migrationBuilder.DropIndex(
                name: "IX_ParticipacoesPartida_PartidaId_Posicao",
                table: "ParticipacoesPartida");

            migrationBuilder.DropColumn(
                name: "EncerradaEmUtc",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "VencedorJogadorId",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "VersaoEstado",
                table: "Partidas");

            migrationBuilder.DropColumn(
                name: "Posicao",
                table: "ParticipacoesPartida");

            migrationBuilder.DropColumn(
                name: "QuantidadePedrasNaMao",
                table: "ParticipacoesPartida");
        }
    }
}
