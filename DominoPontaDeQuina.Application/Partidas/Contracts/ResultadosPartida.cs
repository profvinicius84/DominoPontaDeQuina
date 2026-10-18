namespace DominoPontaDeQuina.Application.Partidas.Contracts;

/// <summary>Partida com uma vaga; não inclui identidade privada ou endereço de callback.</summary>
public sealed record PartidaDisponivel(Guid PartidaId, int PontuacaoAlvo, long VersaoEstado);

/// <summary>Define os resultados semânticos que a camada de transporte mapeará futuramente.</summary>
public enum CodigoResultadoPartida
{
    /// <summary>Operação concluída.</summary>
    Sucesso,

    /// <summary>Entrada inválida.</summary>
    EntradaInvalida,

    /// <summary>Partida não localizada ou indisponível à identidade.</summary>
    PartidaIndisponivel,

    /// <summary>Identidade sem participação ou permissão.</summary>
    SemPermissao,

    /// <summary>Estado atual não admite a ação.</summary>
    EstadoInvalido,

    /// <summary>Ação fora do turno.</summary>
    ForaDoTurno,

    /// <summary>Peça ausente na mão do participante.</summary>
    PecaAusente,

    /// <summary>Peça incompatível com a ponta escolhida.</summary>
    EncaixeInvalido,

    /// <summary>Passagem não autorizada pela cava ou por existir jogada válida.</summary>
    PassagemNaoPermitida,

    /// <summary>Conflito de versão ou ação posterior para o mesmo estado.</summary>
    ConflitoDeEstado
}

/// <summary>Representa a situação autoritativa da partida.</summary>
public enum SituacaoPartida
{
    /// <summary>Aguardando participantes ou início.</summary>
    AguardandoInicio,

    /// <summary>Partida em andamento.</summary>
    EmAndamento,

    /// <summary>Partida encerrada.</summary>
    Encerrada
}

/// <summary>Indica a única próxima ação permitida ao participante no estado projetado.</summary>
public enum ProximaAcaoPartida
{
    /// <summary>O participante deve aguardar uma transição do adversário.</summary>
    Aguardar,

    /// <summary>O participante abre a rodada com a pedra informada.</summary>
    JogarAbertura,

    /// <summary>O participante possui ao menos uma jogada compatível.</summary>
    Jogar,

    /// <summary>O participante não possui encaixe e deve comprar uma pedra.</summary>
    Comprar,

    /// <summary>O participante não possui encaixe nem pedras na cava.</summary>
    Passar,

    /// <summary>A partida está encerrada para o participante.</summary>
    Encerrada
}

/// <summary>Resumo de um participante sem revelar a mão de outro jogador.</summary>
public sealed record ResumoParticipantePartida(
    Guid JogadorId,
    string Nome,
    int Pontuacao,
    int QuantidadePedrasNaMao,
    bool EhVez);

/// <summary>Visão autorizada de estado para o participante solicitante.</summary>
public sealed record EstadoPartidaParticipante(
    Guid PartidaId,
    long VersaoEstado,
    SituacaoPartida Situacao,
    int NumeroRodada,
    Guid? JogadorDoTurnoId,
    IReadOnlyList<PecaPartida> MinhaMao,
    IReadOnlyList<PecaPartida> Tabuleiro,
    IReadOnlyList<ResumoParticipantePartida> Participantes,
    Guid? VencedorId,
    ProximaAcaoPartida ProximaAcao = ProximaAcaoPartida.Aguardar,
    PecaPartida? PedraAberturaObrigatoria = null,
    Guid ParticipacaoId = default);

/// <summary>Visão de acompanhamento sem qualquer pedra de mão ou dado de autenticação.</summary>
public sealed record EstadoPartidaObservador(
    Guid PartidaId,
    long VersaoEstado,
    SituacaoPartida Situacao,
    int NumeroRodada,
    Guid? JogadorDoTurnoId,
    IReadOnlyList<PecaPartida> Tabuleiro,
    IReadOnlyList<ResumoParticipantePartida> Participantes,
    Guid? VencedorId);

/// <summary>Evento histórico permitido para explicar uma ação aceita.</summary>
public sealed record EventoHistoricoPartida(
    Guid EventoId,
    Guid JogadorId,
    PecaPartida? PecaJogada,
    LadoJogavel? Lado,
    bool PassouVez,
    int PontosConcedidos,
    DateTime RecebidoEmUtc);

/// <summary>Resultado final e sequência permitida de eventos da partida.</summary>
public sealed record ResultadoHistoricoPartida(
    Guid PartidaId,
    SituacaoPartida Situacao,
    long VersaoEstado,
    Guid? VencedorId,
    IReadOnlyList<ResumoParticipantePartida> Participantes,
    IReadOnlyList<EventoHistoricoPartida> Eventos);

/// <summary>Confirma a ação aceita e a nova visão autoritativa do participante.</summary>
public sealed record ConfirmacaoAcaoPartida(
    Guid ChaveIdempotencia,
    bool AcaoIdempotente,
    EstadoPartidaParticipante Estado);

/// <summary>
/// Resultado puro de aplicação. Em conflitos, <see cref="EstadoAutoritativo"/>
/// permite substituir a cópia local pela visão filtrada permitida ao solicitante.
/// </summary>
public sealed record ResultadoPartida<T>(
    CodigoResultadoPartida Codigo,
    T? Valor,
    EstadoPartidaParticipante? EstadoAutoritativo)
{
    /// <summary>Indica que o caso de uso foi concluído.</summary>
    public bool Sucesso => Codigo == CodigoResultadoPartida.Sucesso;

    /// <summary>Cria um resultado de sucesso.</summary>
    public static ResultadoPartida<T> CriarSucesso(T valor) =>
        new(CodigoResultadoPartida.Sucesso, valor, null);

    /// <summary>Cria uma rejeição semântica, com estado atual quando aplicável.</summary>
    public static ResultadoPartida<T> CriarFalha(
        CodigoResultadoPartida codigo,
        EstadoPartidaParticipante? estadoAutoritativo = null) =>
        new(codigo, default, estadoAutoritativo);
}
