using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Domain.Repositories;

/// <summary>Define as consultas de ranking por vitórias.</summary>
public interface IRankingRepository : IRepository<Ranking>
{
    /// <summary>Obtém o ranking ordenado pelo número de vitórias.</summary>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Jogadores classificados.</returns>
    Task<IReadOnlyList<Ranking>> ObterAsync(CancellationToken cancellationToken = default);
}
