using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Infrastructure.Persistence.Repositories;

/// <summary>Implementa a persistência de partidas com o <see cref="DominoDbContext"/>.</summary>
public sealed class PartidaRepository(DominoDbContext db) : IPartidaRepository
{
    /// <inheritdoc />
    public async Task<Partida> AdicionarAsync(Partida partida, CancellationToken cancellationToken = default)
    {
        db.Partidas.Add(partida);
        await db.SaveChangesAsync(cancellationToken);
        return partida;
    }

    /// <inheritdoc />
    public async Task<Partida?> ObterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Partidas.AsNoTracking().SingleOrDefaultAsync(partida => partida.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Partida>> ObterHistoricoAsync(CancellationToken cancellationToken = default)
    {
        return await db.Partidas.AsNoTracking().OrderByDescending(partida => partida.CriadaEm)
            .ToListAsync(cancellationToken);
    }
}
