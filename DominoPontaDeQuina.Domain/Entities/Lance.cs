namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>Registra um lance feito por um jogador dentro de uma partida.</summary>
public class Lance
{
    /// <summary>Obtém ou define o identificador do lance.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Obtém ou define o instante do lance em UTC.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Obtém a partida em que o lance ocorreu.</summary>
    public Partida Partida { get; set; } = null!;
    /// <summary>Obtém o jogador que realizou o lance.</summary>
    public Jogador Jogador { get; set; } = null!;
}
