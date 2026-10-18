namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>
/// Representa uma pedra privada no estado recuperável de uma participação.
/// A equivalência e a validade das faces são definidas pelo Core.
/// </summary>
public class PedraMaoPartida
{
    /// <summary>Obtém ou define o identificador da pedra persistida na mão.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Obtém ou define o identificador da partida dona da mão.</summary>
    public Guid PartidaId { get; set; }
    /// <summary>Obtém ou define o identificador do jogador dono da mão.</summary>
    public Guid JogadorId { get; set; }
    /// <summary>Obtém ou define a primeira face persistida.</summary>
    public int ValorA { get; set; }
    /// <summary>Obtém ou define a segunda face persistida.</summary>
    public int ValorB { get; set; }
    /// <summary>Obtém a participação proprietária.</summary>
    public ParticipacaoPartida Participacao { get; set; } = null!;
}
