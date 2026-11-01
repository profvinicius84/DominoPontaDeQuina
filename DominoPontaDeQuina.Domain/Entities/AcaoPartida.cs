namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>
/// Registra a tentativa recebida pelo servidor. Este tipo não valida jogada,
/// turno ou pontuação: tais regras pertencem exclusivamente ao Core.
/// </summary>
public class AcaoPartida
{
    /// <summary>Obtém ou define o identificador da tentativa.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Obtém ou define a partida da tentativa.</summary>
    public Guid PartidaId { get; set; }
    /// <summary>Obtém ou define a rodada relacionada, quando a ação ocorrer numa rodada.</summary>
    public Guid? RodadaPartidaId { get; set; }
    /// <summary>Obtém ou define o autor da tentativa.</summary>
    public Guid AutorJogadorId { get; set; }
    /// <summary>Obtém ou define o tipo recebido.</summary>
    public TipoAcaoPartida Tipo { get; set; }
    /// <summary>Obtém ou define a chave UUID que identifica a tentativa lógica.</summary>
    public Guid ChaveIdempotencia { get; set; }
    /// <summary>Obtém ou define a versão observada pelo cliente.</summary>
    public long VersaoEsperada { get; set; }
    /// <summary>Obtém ou define a versão resultante somente para uma tentativa aceita.</summary>
    public long? VersaoResultante { get; set; }
    /// <summary>Obtém ou define o instante de recebimento gerado pelo servidor, em UTC.</summary>
    public DateTime RecebidaEmUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Obtém ou define a ordem de desempate gerada pelo servidor para a partida.</summary>
    public long OrdemRecebimento { get; set; }
    /// <summary>Obtém ou define se o motor aceitou a tentativa.</summary>
    public bool FoiAceita { get; set; }
    /// <summary>Obtém ou define a primeira face da pedra informada, quando aplicável.</summary>
    public int? ValorA { get; set; }
    /// <summary>Obtém ou define a segunda face da pedra informada, quando aplicável.</summary>
    public int? ValorB { get; set; }
    /// <summary>Obtém ou define a ponta escolhida, quando aplicável.</summary>
    public LadoTabuleiroPartida? Lado { get; set; }
    /// <summary>Obtém ou define os pontos concedidos pelo Core para a ação aceita.</summary>
    public int PontosConcedidos { get; set; }
    /// <summary>Obtém a partida associada.</summary>
    public Partida Partida { get; set; } = null!;
    /// <summary>Obtém a rodada associada, quando houver.</summary>
    public RodadaPartida? RodadaPartida { get; set; }
    /// <summary>Obtém o jogador autor, quando carregado.</summary>
    public Jogador Autor { get; set; } = null!;

    /// <summary>Valida somente a coerência do registro persistente, sem reexecutar o motor.</summary>
    public void ValidarCoerencia()
    {
        if (PartidaId == Guid.Empty || AutorJogadorId == Guid.Empty || ChaveIdempotencia == Guid.Empty)
            throw new InvalidOperationException("Partida, autor e chave de idempotência são obrigatórios.");
        if (OrdemRecebimento < 1)
            throw new InvalidOperationException("A ordem de recebimento deve ser positiva.");
        if (RecebidaEmUtc.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException("O recebimento deve ser registrado em UTC.");
        if (FoiAceita && VersaoResultante != VersaoEsperada + 1)
            throw new InvalidOperationException("Uma ação aceita deve avançar exatamente uma versão.");
        if (!FoiAceita && VersaoResultante is not null)
            throw new InvalidOperationException("Uma ação rejeitada não possui versão resultante.");
    }
}

/// <summary>Indica a intenção persistida para histórico e idempotência.</summary>
public enum TipoAcaoPartida
{
    Ingresso,
    Inicio,
    Compra,
    Jogada,
    Passagem,
    EncerramentoRodada,
    EncerramentoPartida,
    CancelamentoPartida
}

/// <summary>Indica a ponta do tabuleiro registrada pela ação, sem decisão de interface.</summary>
public enum LadoTabuleiroPartida
{
    Esquerda,
    Direita
}
