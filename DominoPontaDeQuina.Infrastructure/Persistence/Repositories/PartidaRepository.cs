using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Infrastructure.Persistence.Repositories;

/// <summary>Implementa a persistência de partidas com o <see cref="DominoDbContext"/>.</summary>
public sealed class PartidaRepository : EfRepository<Partida>, IPartidaRepository
{
    readonly DominoDbContext db;

    /// <summary>Inicializa o repositório de partidas.</summary>
    public PartidaRepository(DominoDbContext db) : base(db) => this.db = db;
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
