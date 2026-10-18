namespace DominoPontaDeQuina.Application.Partidas.Contracts;

/// <summary>
/// Define a jornada jogável por contratos de aplicação, independente de HTTP ou interface.
/// A implementação transacional e a integração com o motor serão realizadas na T-16.
/// </summary>
public interface IJornadaPartidaService
{
    Task<IReadOnlyList<PartidaDisponivel>> ListarDisponiveisAsync(CancellationToken cancellationToken = default);
    /// <summary>Cria uma partida para a identidade solicitante.</summary>
    Task<ResultadoPartida<EstadoPartidaParticipante>> CriarAsync(
        CriarPartidaComando comando,
        CancellationToken cancellationToken = default);

    /// <summary>Registra a participação do solicitante na partida.</summary>
    Task<ResultadoPartida<ConfirmacaoAcaoPartida>> IngressarAsync(
        IngressarPartidaComando comando,
        CancellationToken cancellationToken = default);

    /// <summary>Inicia a partida quando suas pré-condições estiverem atendidas.</summary>
    Task<ResultadoPartida<ConfirmacaoAcaoPartida>> IniciarAsync(
        IniciarPartidaComando comando,
        CancellationToken cancellationToken = default);

    /// <summary>Consulta a visão autorizada do participante.</summary>
    Task<ResultadoPartida<EstadoPartidaParticipante>> ConsultarEstadoProprioAsync(
        ConsultarEstadoProprioConsulta consulta,
        CancellationToken cancellationToken = default);

    /// <summary>Consulta a visão pública e segura de observação da partida.</summary>
    Task<ResultadoPartida<EstadoPartidaObservador>> ConsultarObservacaoAsync(
        ConsultarObservacaoConsulta consulta,
        CancellationToken cancellationToken = default);

    /// <summary>Executa uma jogada para a versão do estado observada.</summary>
    Task<ResultadoPartida<ConfirmacaoAcaoPartida>> JogarAsync(
        JogarPartidaComando comando,
        CancellationToken cancellationToken = default);

    /// <summary>Compra uma única pedra quando não houver jogada e passa apenas se necessário.</summary>
    Task<ResultadoPartida<ConfirmacaoAcaoPartida>> ComprarAsync(
        ComprarPartidaComando comando,
        CancellationToken cancellationToken = default);

    /// <summary>Passa a vez quando a regra permitir.</summary>
    Task<ResultadoPartida<ConfirmacaoAcaoPartida>> PassarVezAsync(
        PassarVezPartidaComando comando,
        CancellationToken cancellationToken = default);

    /// <summary>Consulta resultado e histórico filtrados pela participação.</summary>
    Task<ResultadoPartida<ResultadoHistoricoPartida>> ConsultarResultadoHistoricoAsync(
        ConsultarResultadoHistoricoConsulta consulta,
        CancellationToken cancellationToken = default);
}
