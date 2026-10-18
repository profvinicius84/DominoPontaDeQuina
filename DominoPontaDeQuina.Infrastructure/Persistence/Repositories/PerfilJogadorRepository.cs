using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Infrastructure.Persistence.Repositories;

public sealed class PerfilJogadorRepository(DominoDbContext db) : IPerfilJogadorRepository
{
    public async Task<IReadOnlyList<Jogador>> ListarAsync(Guid usuarioId, CancellationToken ct = default) =>
        await db.Jogadores.AsNoTracking().Where(j => j.UsuarioId == usuarioId)
            .OrderBy(j => j.Nome).ThenBy(j => j.Id).ToListAsync(ct);

    public Task<Jogador?> ObterAsync(Guid usuarioId, Guid jogadorId, CancellationToken ct = default) =>
        db.Jogadores.AsNoTracking().SingleOrDefaultAsync(j => j.UsuarioId == usuarioId && j.Id == jogadorId, ct);

    public async Task<Jogador> CriarAsync(Guid usuarioId, string nome, CancellationToken ct = default)
    {
        var jogador = new Jogador { UsuarioId = usuarioId, Nome = nome };
        db.Jogadores.Add(jogador);
        await db.SaveChangesAsync(ct);
        return jogador;
    }

    public async Task<bool> TentarAtivarAsync(Guid usuarioId, Guid jogadorId, string hash, DateTime agoraUtc, CancellationToken ct = default)
    {
        // Um UPDATE condicional impede duas ativações mesmo em hosts diferentes.
        var alterados = await db.Jogadores.Where(j => j.UsuarioId == usuarioId && j.Id == jogadorId && j.AtivacaoHash == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(j => j.AtivacaoHash, hash)
                .SetProperty(j => j.AtivadoEmUtc, agoraUtc), ct);
        return alterados == 1 || await db.Jogadores.AsNoTracking()
            .AnyAsync(j => j.UsuarioId == usuarioId && j.Id == jogadorId && j.AtivacaoHash == hash, ct);
    }

    public async Task<Jogador?> ObterAtivoAsync(Guid usuarioId, Guid jogadorId, string hash, CancellationToken ct = default)
    {
        var jogador = await db.Jogadores.SingleOrDefaultAsync(j => j.UsuarioId == usuarioId && j.Id == jogadorId && j.AtivacaoHash == hash, ct);
        if (jogador is not null)
            db.Entry(jogador).Property(j => j.AtivacaoHash).IsModified = true;
        return jogador;
    }

    public async Task<bool> TentarLiberarAsync(Guid usuarioId, Guid jogadorId, string hash, CancellationToken ct = default) =>
        await db.Jogadores.Where(j => j.UsuarioId == usuarioId && j.Id == jogadorId && j.AtivacaoHash == hash)
            .ExecuteUpdateAsync(setters => setters.SetProperty(j => j.AtivacaoHash, (string?)null)
                .SetProperty(j => j.AtivadoEmUtc, (DateTime?)null), ct) == 1;
}
