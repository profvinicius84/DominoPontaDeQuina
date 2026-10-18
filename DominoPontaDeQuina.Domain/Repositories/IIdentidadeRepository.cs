using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Domain.Repositories;

/// <summary>Persistência interna de usuários autorizados e credenciais revogáveis.</summary>
public interface IIdentidadeRepository
{
    Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<(Usuario Usuario, bool Adicionado)> AdicionarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task EmitirTokenAsync(TokenAcesso token, CancellationToken cancellationToken = default);
    Task<TokenAcesso?> ObterTokenValidoPorHashAsync(string hash, CancellationToken cancellationToken = default);
    Task<bool> RevogarTokenAsync(Guid usuarioId, string hash, DateTime agoraUtc, CancellationToken cancellationToken = default);
}
