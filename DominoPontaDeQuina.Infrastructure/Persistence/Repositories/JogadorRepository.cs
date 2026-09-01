using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Infrastructure.Persistence.Repositories;

/// <summary>Implementa a persistência de jogadores com o <see cref="DominoDbContext"/>.</summary>
public sealed class JogadorRepository(DominoDbContext db) : IJogadorRepository
{
    /// <inheritdoc />
    public async Task<Jogador> AdicionarAsync(Jogador jogador, Guid? usuarioId, CancellationToken cancellationToken = default)
    {
        db.Jogadores.Add(jogador);
        db.Entry(jogador).Property<Guid?>("UsuarioId").CurrentValue = usuarioId;
        await db.SaveChangesAsync(cancellationToken);
        return jogador;
    }

    /// <inheritdoc />
    public Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Jogadores.AnyAsync(jogador => jogador.Id == id, cancellationToken);
}
