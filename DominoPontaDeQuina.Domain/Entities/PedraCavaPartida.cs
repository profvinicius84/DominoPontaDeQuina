namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>Representa uma pedra da cava com a ordem necessária para retomar compras.</summary>
public class PedraCavaPartida
{
    /// <summary>Identificador da pedra persistida.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Rodada proprietária da cava.</summary>
    public Guid RodadaPartidaId { get; set; }
    /// <summary>Ordem de retirada da pedra na cava.</summary>
    public int Ordem { get; set; }
    /// <summary>Primeira face da pedra.</summary>
    public int ValorA { get; set; }
    /// <summary>Segunda face da pedra.</summary>
    public int ValorB { get; set; }
    /// <summary>Rodada relacionada.</summary>
    public RodadaPartida RodadaPartida { get; set; } = null!;
}
