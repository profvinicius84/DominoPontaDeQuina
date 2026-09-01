using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Domain.Repositories;

/// <summary>Define as operações de persistência de <see cref="Entities.Jogador"/>.</summary>
public interface IJogadorRepository : IRepository<Jogador>
{
    /// <summary>Adiciona um jogador e associa opcionalmente uma conta de usuário.</summary>
    /// <param name="jogador">Jogador a ser persistido.</param>
    /// <param name="usuarioId">Identificador opcional da conta.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O jogador persistido.</returns>
    Task<Jogador> AdicionarComUsuarioAsync(Jogador jogador, Guid? usuarioId, CancellationToken cancellationToken = default);
    /// <summary>Verifica se um jogador existe.</summary>
    /// <param name="id">Identificador do jogador.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns><see langword="true"/> quando o jogador existe.</returns>
    Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default);
}
