using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Infrastructure.Persistence.Repositories;

public sealed class EventoWebhookRepository(DominoDbContext db) : IEventoWebhookRepository
{
    public async Task<EventoWebhook?> ReservarProximoAsync(DateTime agoraUtc, TimeSpan duracaoReserva, CancellationToken ct = default)
    {
        var candidatos = await db.EventosWebhook.AsNoTracking()
            .Where(e => e.EntregueEmUtc == null && e.AbandonadoEmUtc == null
                && e.ProximaTentativaEmUtc <= agoraUtc
                && (e.EmProcessamentoAteUtc == null || e.EmProcessamentoAteUtc <= agoraUtc))
            .OrderBy(e => e.ProximaTentativaEmUtc).ThenBy(e => e.CriadoEmUtc)
            .Select(e => e.Id).Take(8).ToArrayAsync(ct);

        foreach (var id in candidatos)
        {
            var reservados = await db.EventosWebhook.Where(e => e.Id == id
                    && e.EntregueEmUtc == null && e.AbandonadoEmUtc == null
                    && e.ProximaTentativaEmUtc <= agoraUtc
                    && (e.EmProcessamentoAteUtc == null || e.EmProcessamentoAteUtc <= agoraUtc))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(e => e.EmProcessamentoAteUtc, agoraUtc.Add(duracaoReserva))
                    .SetProperty(e => e.Tentativas, e => e.Tentativas + 1), ct);
            if (reservados == 1)
                return await db.EventosWebhook.AsNoTracking().SingleAsync(e => e.Id == id, ct);
        }

        return null;
    }

    public Task MarcarEntregueAsync(Guid eventoId, DateTime entregueEmUtc, CancellationToken ct = default) =>
        AtualizarAsync(eventoId, entregueEmUtc, null, null, false, ct);

    public Task MarcarFalhaAsync(Guid eventoId, DateTime proximaTentativaEmUtc, string falhaSegura, bool abandonar, CancellationToken ct = default) =>
        AtualizarAsync(eventoId, null, proximaTentativaEmUtc, falhaSegura, abandonar, ct);

    public Task<int> RemoverExpiradosAsync(DateTime entreguesAntesUtc, DateTime abandonadosAntesUtc, CancellationToken ct = default) =>
        db.EventosWebhook.Where(e => (e.EntregueEmUtc != null && e.EntregueEmUtc < entreguesAntesUtc)
            || (e.AbandonadoEmUtc != null && e.AbandonadoEmUtc < abandonadosAntesUtc)).ExecuteDeleteAsync(ct);

    private async Task AtualizarAsync(Guid id, DateTime? entregue, DateTime? proxima, string? falha, bool abandonar, CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        await db.EventosWebhook.Where(e => e.Id == id).ExecuteUpdateAsync(setters => setters
            .SetProperty(e => e.EntregueEmUtc, entregue)
            .SetProperty(e => e.ProximaTentativaEmUtc, proxima ?? agora)
            .SetProperty(e => e.UltimaFalha, falha)
            .SetProperty(e => e.AbandonadoEmUtc, abandonar ? agora : null)
            .SetProperty(e => e.EmProcessamentoAteUtc, (DateTime?)null), ct);
    }
}
