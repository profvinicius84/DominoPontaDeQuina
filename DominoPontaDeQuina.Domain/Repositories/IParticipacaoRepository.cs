using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Domain.Repositories;

/// <summary>Define as operações de persistência de participações em partidas.</summary>
public interface IParticipacaoRepository
{
    /// <summary>Adiciona uma participação para um jogador em uma partida.</summary>
    /// <param name="participacao">Dados da participação.</param>
    /// <param name="partidaId">Identificador da partida.</param>
    /// <param name="jogadorId">Identificador do jogador.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    Task AdicionarAsync(ParticipacaoPartida participacao, Guid partidaId, Guid jogadorId, CancellationToken cancellationToken = default);
}
