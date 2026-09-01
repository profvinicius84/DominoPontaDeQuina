using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Application.Services;

/// <summary>Expõe os casos de uso de gerenciamento de partidas.</summary>
public interface IPartidaService
{
    /// <summary>Inicia uma nova partida.</summary>
    /// <param name="pontuacaoAlvo">Pontuação necessária para vencer.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A partida iniciada.</returns>
    Task<Partida> IniciarPartidaAsync(int pontuacaoAlvo = 50, CancellationToken cancellationToken = default);
    /// <summary>Registra um jogador e sua participação inicial na partida.</summary>
    /// <param name="partidaId">Identificador da partida.</param>
    /// <param name="nome">Nome do jogador.</param>
    /// <param name="usuarioId">Conta opcional associada ao jogador.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O jogador registrado.</returns>
    Task<Jogador> RegistrarJogadorAsync(Guid partidaId, string nome, Guid? usuarioId = null, CancellationToken cancellationToken = default);
    /// <summary>Registra um lance realizado por um jogador.</summary>
    /// <param name="partidaId">Identificador da partida.</param>
    /// <param name="jogadorId">Identificador do jogador.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O lance registrado.</returns>
    Task<Lance> RegistrarLanceAsync(Guid partidaId, Guid jogadorId, CancellationToken cancellationToken = default);
    /// <summary>Consulta o status atual de uma partida.</summary>
    /// <param name="partidaId">Identificador da partida.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A partida consultada.</returns>
    Task<Partida> VerificarStatusAsync(Guid partidaId, CancellationToken cancellationToken = default);
    /// <summary>Consulta o ranking de jogadores por vitórias.</summary>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Ranking ordenado.</returns>
    Task<IReadOnlyList<Ranking>> ConsultarRankingAsync(CancellationToken cancellationToken = default);
    /// <summary>Consulta o histórico de partidas.</summary>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Partidas do histórico.</returns>
    Task<IReadOnlyList<Partida>> ConsultarHistoricoAsync(CancellationToken cancellationToken = default);
}
