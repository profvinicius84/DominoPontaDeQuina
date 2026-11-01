namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>Representa o estado persistente de uma rodada; as regras continuam no Core.</summary>
public class RodadaPartida
{
    /// <summary>Obtém ou define o identificador da rodada.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Obtém ou define a partida à qual a rodada pertence.</summary>
    public Guid PartidaId { get; set; }
    /// <summary>Obtém ou define o número sequencial da rodada na partida.</summary>
    public int Numero { get; set; }
    /// <summary>Obtém ou define a situação persistida da rodada.</summary>
    public SituacaoRodadaPartida Situacao { get; set; } = SituacaoRodadaPartida.AguardandoInicio;
    /// <summary>Obtém ou define o participante que possui o turno persistido.</summary>
    public Guid? JogadorDoTurnoId { get; set; }
    /// <summary>Obtém ou define a quantidade de pedras remanescentes na cava.</summary>
    public int QuantidadeNaCava { get; set; }
    /// <summary>Obtém ou define a ponta esquerda atual do tabuleiro.</summary>
    public int? PontaEsquerda { get; set; }
    /// <summary>Obtém ou define a ponta direita atual do tabuleiro.</summary>
    public int? PontaDireita { get; set; }
    /// <summary>Obtém ou define a primeira face da pedra obrigatória de abertura.</summary>
    public int? PedraAberturaValorA { get; set; }
    /// <summary>Obtém ou define a segunda face da pedra obrigatória de abertura.</summary>
    public int? PedraAberturaValorB { get; set; }
    /// <summary>Obtém ou define o jogador vencedor da rodada, quando houver.</summary>
    public Guid? VencedorJogadorId { get; set; }
    /// <summary>Obtém ou define como a rodada foi encerrada.</summary>
    public MotivoEncerramentoRodada? MotivoEncerramento { get; set; }
    /// <summary>Obtém ou define o instante de início em UTC.</summary>
    public DateTime IniciadaEmUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Obtém ou define o instante de encerramento em UTC, quando houver.</summary>
    public DateTime? EncerradaEmUtc { get; set; }
    /// <summary>Obtém a partida associada.</summary>
    public Partida Partida { get; set; } = null!;
    /// <summary>Obtém as ações da rodada, ordenadas por <see cref="AcaoPartida.OrdemRecebimento"/>.</summary>
    public ICollection<AcaoPartida> Acoes { get; set; } = [];
    /// <summary>Obtém as pedras restantes na cava, preservadas na ordem de compra.</summary>
    public ICollection<PedraCavaPartida> Cava { get; set; } = [];
}

/// <summary>Indica a situação persistida de uma rodada.</summary>
public enum SituacaoRodadaPartida
{
    AguardandoInicio,
    EmAndamento,
    Encerrada
}

/// <summary>Indica o término observado pelo Core para uma rodada encerrada.</summary>
public enum MotivoEncerramentoRodada
{
    MaoVazia,
    TabuleiroTravado,
    CancelamentoPartida,
    ReinicioPartida
}
