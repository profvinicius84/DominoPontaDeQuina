using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Domain.Repositories;

/// <summary>
/// Define a leitura do agregado jogável. A implementação deve carregar as ações
/// em ordem de recebimento e não transforma este contrato em uma visão pública.
/// </summary>
public interface IEstadoPartidaRepository
{
    /// <summary>Lista somente partidas aguardando ingresso com uma posição ocupada, sem mãos.</summary>
    Task<IReadOnlyList<Partida>> ListarDisponiveisAsync(CancellationToken cancellationToken = default);
    /// <summary>Indica se o jogador já ocupa uma partida ainda não encerrada.</summary>
    Task<bool> PossuiParticipacaoNaoEncerradaAsync(Guid jogadorId, Guid? excetoPartidaId = null, CancellationToken cancellationToken = default);
    /// <summary>Obtém a partida com participantes, mãos, rodadas e ações necessárias à recuperação.</summary>
    Task<Partida?> ObterParaRecuperacaoAsync(Guid partidaId, CancellationToken cancellationToken = default);

    /// <summary>Obtém o agregado rastreado para uma transição atômica de estado.</summary>
    Task<Partida?> ObterParaAtualizacaoAsync(Guid partidaId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um agregado novo em uma única persistência.</summary>
    Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenta persistir a transição rastreada. Um conflito de token de concorrência
    /// não confirma a ação e não deve ser convertido em sucesso pelo chamador.
    /// </summary>
    Task<bool> TentarSalvarAsync(CancellationToken cancellationToken = default);

    /// <summary>Descarta a transição local após conflito para permitir a leitura autoritativa.</summary>
    void DescartarAlteracoes();

    /// <summary>Marca uma entidade recém-criada do agregado para inclusão na transação atual.</summary>
    void AdicionarAoContexto(object entidade);

    /// <summary>
    /// Obtém a visão persistida de um participante, carregando somente a mão do
    /// jogador informado; a autorização continua sendo aplicada pela Application.
    /// </summary>
    Task<Partida?> ObterParaParticipanteAsync(
        Guid partidaId,
        Guid jogadorId,
        CancellationToken cancellationToken = default);

    /// <summary>Obtém a visão de observação, sem carregar mãos privadas.</summary>
    Task<Partida?> ObterParaObservadorAsync(Guid partidaId, CancellationToken cancellationToken = default);

    /// <summary>Localiza uma tentativa lógica já recebida para garantir idempotência.</summary>
    Task<AcaoPartida?> ObterPorChaveIdempotenciaAsync(
        Guid partidaId,
        Guid autorJogadorId,
        Guid chaveIdempotencia,
        CancellationToken cancellationToken = default);
}
