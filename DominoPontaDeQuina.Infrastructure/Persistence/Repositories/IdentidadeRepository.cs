using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Infrastructure.Persistence.Repositories;

/// <summary>Persiste usuários autorizados e hashes de tokens sem registrar o segredo.</summary>
public sealed class IdentidadeRepository(DominoDbContext db) : IIdentidadeRepository
{
    public Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken cancellationToken = default) =>
        db.Usuarios.SingleOrDefaultAsync(usuario => usuario.Email == emailNormalizado, cancellationToken);
    public Task<Usuario?> ObterPorIdAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        db.Usuarios.SingleOrDefaultAsync(usuario => usuario.Id == usuarioId, cancellationToken);
    public async Task<(Usuario Usuario, bool Adicionado)> AdicionarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        db.Usuarios.Add(usuario);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return (usuario, true);
        }
        catch (DbUpdateException)
        {
            db.Entry(usuario).State = EntityState.Detached;
            var existente = await ObterPorEmailAsync(usuario.Email, cancellationToken);
            if (existente is not null) return (existente, false);
            throw;
        }
    }
    public async Task EmitirTokenAsync(TokenAcesso token, CancellationToken cancellationToken = default)
    {
        db.TokensAcesso.Add(token);
        await db.SaveChangesAsync(cancellationToken);
    }
    public Task<TokenAcesso?> ObterTokenValidoPorHashAsync(string hash, CancellationToken cancellationToken = default) =>
        db.TokensAcesso.AsNoTracking().SingleOrDefaultAsync(token => token.Hash == hash && token.RevogadoEmUtc == null, cancellationToken);
    public async Task<bool> RevogarTokenAsync(Guid usuarioId, string hash, DateTime agoraUtc, CancellationToken cancellationToken = default)
    {
        var token = await db.TokensAcesso.SingleOrDefaultAsync(item => item.UsuarioId == usuarioId && item.Hash == hash && item.RevogadoEmUtc == null, cancellationToken);
        if (token is null) return false;
        token.RevogadoEmUtc = agoraUtc;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
