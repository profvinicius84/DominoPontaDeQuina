using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Domain.Repositories;

/// <summary>Define as operações de persistência de <see cref="Entities.Partida"/>.</summary>
public interface IPartidaRepository : IRepository<Partida>
{
    /// <summary>Obtém uma partida pelo identificador.</summary>
    /// <param name="id">Identificador da partida.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A partida ou <see langword="null"/> quando não encontrada.</returns>
    Task<Partida?> ObterAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Obtém as partidas ordenadas da mais recente para a mais antiga.</summary>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Histórico de partidas.</returns>
    Task<IReadOnlyList<Partida>> ObterHistoricoAsync(CancellationToken cancellationToken = default);
}
