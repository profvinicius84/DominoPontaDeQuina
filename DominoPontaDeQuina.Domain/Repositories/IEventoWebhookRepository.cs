using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Domain.Repositories;

/// <summary>Reserva e conclui entregas da caixa de saída sem expor segredos do cliente.</summary>
public interface IEventoWebhookRepository
{
    Task<EventoWebhook?> ReservarProximoAsync(DateTime agoraUtc, TimeSpan duracaoReserva, CancellationToken ct = default);
    Task MarcarEntregueAsync(Guid eventoId, DateTime entregueEmUtc, CancellationToken ct = default);
    Task MarcarFalhaAsync(Guid eventoId, DateTime proximaTentativaEmUtc, string falhaSegura, bool abandonar, CancellationToken ct = default);
    Task<int> RemoverExpiradosAsync(DateTime entreguesAntesUtc, DateTime abandonadosAntesUtc, CancellationToken ct = default);
}
