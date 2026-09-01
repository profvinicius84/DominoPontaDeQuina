namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>Representa o ranking de vitórias de um <see cref="Jogador"/>.</summary>
public class Ranking
{
    /// <summary>Obtém ou define o número de vitórias do jogador.</summary>
    public int Vitorias { get; set; }

    /// <summary>Obtém o jogador classificado.</summary>
    public Jogador Jogador { get; set; } = null!;
}
