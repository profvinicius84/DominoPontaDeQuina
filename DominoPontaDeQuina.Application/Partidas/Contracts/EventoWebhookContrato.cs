namespace DominoPontaDeQuina.Application.Partidas.Contracts;

/// <summary>Envelope versionado entregue ao callback privado de uma participação.</summary>
public sealed record EventoEstadoPartidaWebhook(
    Guid EventoId,
    Guid PartidaId,
    Guid ParticipacaoId,
    long VersaoEstado,
    string Tipo,
    DateTime OcorridoEmUtc,
    EstadoPartidaParticipante Estado);
