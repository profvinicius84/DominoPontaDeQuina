namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>Representa um token reutilizável até revogação pelo seu hash, nunca pelo segredo em texto.</summary>
public class TokenAcesso
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public string Hash { get; set; } = string.Empty;
    public DateTime EmitidoEmUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevogadoEmUtc { get; set; }
    public Usuario Usuario { get; set; } = null!;
}
