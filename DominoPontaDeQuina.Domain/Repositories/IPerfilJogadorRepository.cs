using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Domain.Repositories;

/// <summary>Perfis da conta e reserva atômica de ativação por jogador.</summary>
public interface IPerfilJogadorRepository
{
    Task<IReadOnlyList<Jogador>> ListarAsync(Guid usuarioId, CancellationToken ct = default);
    Task<Jogador?> ObterAsync(Guid usuarioId, Guid jogadorId, CancellationToken ct = default);
    Task<Jogador> CriarAsync(Guid usuarioId, string nome, CancellationToken ct = default);
    Task<bool> TentarAtivarAsync(Guid usuarioId, Guid jogadorId, string hash, DateTime agoraUtc, CancellationToken ct = default);
    Task<bool> TentarLiberarAsync(Guid usuarioId, Guid jogadorId, string hash, CancellationToken ct = default);
    Task<Jogador?> ObterAtivoAsync(Guid usuarioId, Guid jogadorId, string hash, CancellationToken ct = default);
}
