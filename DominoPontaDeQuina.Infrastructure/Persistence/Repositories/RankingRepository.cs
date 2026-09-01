using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Infrastructure.Persistence.Repositories;

/// <summary>Implementa a consulta de ranking com o <see cref="DominoDbContext"/>.</summary>
public sealed class RankingRepository(DominoDbContext db) : IRankingRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Ranking>> ObterAsync(CancellationToken cancellationToken = default)
    {
        return await db.Rankings.AsNoTracking()
            .OrderByDescending(ranking => ranking.Vitorias)
            .ThenBy(ranking => ranking.Jogador.Nome)
            .ToListAsync(cancellationToken);
    }
}
