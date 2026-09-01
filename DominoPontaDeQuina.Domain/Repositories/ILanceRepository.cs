using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Domain.Repositories;

/// <summary>Define as operações de persistência de <see cref="Entities.Lance"/>.</summary>
public interface ILanceRepository : IRepository<Lance>
{
    /// <summary>Adiciona um lance associado a jogador e partida.</summary>
    /// <param name="lance">Lance a ser persistido.</param>
    /// <param name="partidaId">Identificador da partida.</param>
    /// <param name="jogadorId">Identificador do jogador.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O lance persistido.</returns>
    Task<Lance> AdicionarNaPartidaAsync(Lance lance, Guid partidaId, Guid jogadorId, CancellationToken cancellationToken = default);
}
