using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Domain.Repositories;

/// <summary>Define as operações de persistência de participações em partidas.</summary>
public interface IParticipacaoRepository : IRepository<ParticipacaoPartida>
{
    /// <summary>Adiciona uma participação para um jogador em uma partida.</summary>
    /// <param name="participacao">Dados da participação.</param>
    /// <param name="partidaId">Identificador da partida.</param>
    /// <param name="jogadorId">Identificador do jogador.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    Task AdicionarNaPartidaAsync(ParticipacaoPartida participacao, Guid partidaId, Guid jogadorId, CancellationToken cancellationToken = default);
}
