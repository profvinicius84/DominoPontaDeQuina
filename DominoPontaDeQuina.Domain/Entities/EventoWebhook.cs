namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>Evento privado persistido na mesma transação da mudança de estado.</summary>
public sealed class EventoWebhook
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PartidaId { get; set; }
    public Guid JogadorId { get; set; }
    public long VersaoEstado { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string ConteudoJson { get; set; } = string.Empty;
    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
    public DateTime ProximaTentativaEmUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EmProcessamentoAteUtc { get; set; }
    public int Tentativas { get; set; }
    public DateTime? EntregueEmUtc { get; set; }
    public DateTime? AbandonadoEmUtc { get; set; }
    public string? UltimaFalha { get; set; }
}
