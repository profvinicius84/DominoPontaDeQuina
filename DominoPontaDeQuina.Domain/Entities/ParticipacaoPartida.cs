namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>Representa a participação de um <see cref="Jogador"/> em uma <see cref="Partida"/>.</summary>
public class ParticipacaoPartida
{
    /// <summary>Obtém ou define a pontuação acumulada na participação.</summary>
    public int Pontos { get; set; }
    /// <summary>Obtém a partida relacionada.</summary>
    public Partida Partida { get; set; } = null!;
    /// <summary>Obtém o jogador relacionado.</summary>
    public Jogador Jogador { get; set; } = null!;
}
