using DominoPontaDeQuina.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DominoPontaDeQuina.Migrations.Persistence.Migrations;

/// <summary>Acrescenta ativação exclusiva do perfil e destino privado por participação.</summary>
[DbContext(typeof(DominoDbContext))]
[Migration("20260923164646_PerfisJogadorAtivacaoWebhook")]
public sealed class PerfisJogadorAtivacaoWebhook : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AtivacaoHash", table: "Jogadores", type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<DateTime>(
            name: "AtivadoEmUtc", table: "Jogadores", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "WebhookUrl", table: "ParticipacoesPartida", type: "nvarchar(2048)", maxLength: 2048, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AtivacaoHash", table: "Jogadores");
        migrationBuilder.DropColumn(name: "AtivadoEmUtc", table: "Jogadores");
        migrationBuilder.DropColumn(name: "WebhookUrl", table: "ParticipacoesPartida");
    }
}
