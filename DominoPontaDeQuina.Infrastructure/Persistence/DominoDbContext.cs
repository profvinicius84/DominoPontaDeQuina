using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Infrastructure.Persistence;

/// <summary>Representa a sessão do EF Core com o banco do Dominó.</summary>
public class DominoDbContext(DbContextOptions<DominoDbContext> options) : DbContext(options)
{
    /// <summary>Obtém as contas de usuário persistidas.</summary>
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    /// <summary>Obtém os hashes de acessos reutilizáveis até revogação.</summary>
    public DbSet<TokenAcesso> TokensAcesso => Set<TokenAcesso>();
    /// <summary>Obtém as partidas persistidas.</summary>
    public DbSet<Partida> Partidas => Set<Partida>();
    /// <summary>Obtém os jogadores persistidos.</summary>
    public DbSet<Jogador> Jogadores => Set<Jogador>();
    /// <summary>Obtém as participações persistidas.</summary>
    public DbSet<ParticipacaoPartida> ParticipacoesPartida => Set<ParticipacaoPartida>();
    /// <summary>Obtém os lances persistidos.</summary>
    public DbSet<Lance> Lances => Set<Lance>();
    /// <summary>Obtém as rodadas persistidas.</summary>
    public DbSet<RodadaPartida> RodadasPartida => Set<RodadaPartida>();
    /// <summary>Obtém as tentativas de ação recebidas.</summary>
    public DbSet<AcaoPartida> AcoesPartida => Set<AcaoPartida>();
    /// <summary>Obtém as pedras privadas atualmente nas mãos dos participantes.</summary>
    public DbSet<PedraMaoPartida> PedrasMaoPartida => Set<PedraMaoPartida>();
    /// <summary>Obtém as pedras das cavas persistidas.</summary>
    public DbSet<PedraCavaPartida> PedrasCavaPartida => Set<PedraCavaPartida>();
    /// <summary>Obtém a caixa de saída persistente de notificações privadas.</summary>
    public DbSet<EventoWebhook> EventosWebhook => Set<EventoWebhook>();
    /// <summary>Obtém os rankings persistidos.</summary>
    public DbSet<Ranking> Rankings => Set<Ranking>();

    /// <summary>Aplica todas as configurações Fluent API da infraestrutura.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DominoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
