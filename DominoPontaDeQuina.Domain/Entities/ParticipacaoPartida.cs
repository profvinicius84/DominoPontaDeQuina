namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>Representa a participação de um <see cref="Jogador"/> em uma <see cref="Partida"/>.</summary>
public class ParticipacaoPartida
{
    /// <summary>Identificador público atribuído somente pela API.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Obtém ou define o identificador da partida da participação.</summary>
    public Guid PartidaId { get; set; }
    /// <summary>Obtém ou define o identificador do jogador participante.</summary>
    public Guid JogadorId { get; set; }
    /// <summary>Obtém ou define a posição estável do jogador (um ou dois).</summary>
    public int Posicao { get; set; }
    /// <summary>Destino registrado pelo cliente para futuras notificações privadas.</summary>
    public string? WebhookUrl { get; set; }
    /// <summary>Obtém ou define a pontuação acumulada na participação.</summary>
    public int Pontos { get; set; }
    /// <summary>Obtém ou define a contagem privada da mão para visões sem as pedras.</summary>
    public int QuantidadePedrasNaMao { get; set; }
    /// <summary>Obtém a partida relacionada.</summary>
    public Partida Partida { get; set; } = null!;
    /// <summary>Obtém o jogador relacionado.</summary>
    public Jogador Jogador { get; set; } = null!;
    /// <summary>Obtém as pedras privadas atualmente atribuídas ao participante.</summary>
    public ICollection<PedraMaoPartida> Mao { get; set; } = [];
}
