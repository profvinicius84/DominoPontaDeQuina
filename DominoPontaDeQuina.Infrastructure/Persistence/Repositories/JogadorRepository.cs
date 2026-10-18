using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Infrastructure.Persistence.Repositories;

/// <summary>Implementa a persistência de jogadores com o <see cref="DominoDbContext"/>.</summary>
public sealed class JogadorRepository : EfRepository<Jogador>, IJogadorRepository
{
    readonly DominoDbContext db;

    /// <summary>Inicializa o repositório de jogadores.</summary>
    public JogadorRepository(DominoDbContext db) : base(db) => this.db = db;
    /// <inheritdoc />
    public async Task<Jogador> AdicionarComUsuarioAsync(Jogador jogador, Guid? usuarioId, CancellationToken cancellationToken = default)
    {
        db.Jogadores.Add(jogador);
        jogador.UsuarioId = usuarioId;
        await db.SaveChangesAsync(cancellationToken);
        return jogador;
    }

    /// <inheritdoc />
    public Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Jogadores.AnyAsync(jogador => jogador.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<Jogador?> ObterPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        // Compatibilidade dos casos de uso antigos: nunca escolher um perfil arbitrário.
        var perfis = await db.Jogadores.Where(jogador => jogador.UsuarioId == usuarioId)
            .Take(2).ToListAsync(cancellationToken);
        return perfis.Count == 1 && perfis[0].AtivacaoHash is null ? perfis[0] : null;
    }
}
