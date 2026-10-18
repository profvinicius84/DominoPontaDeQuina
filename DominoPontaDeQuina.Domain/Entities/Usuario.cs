namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>Representa a conta de usuário usada na autenticação da aplicação.</summary>
public class Usuario
{
    /// <summary>Obtém ou define o identificador da conta.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Obtém ou define o e-mail usado para autenticação.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Obtém ou define o hash da senha, nunca a senha em texto puro.</summary>
    public string SenhaHash { get; set; } = string.Empty;
    /// <summary>Obtém ou define a data de criação da conta em UTC.</summary>
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    /// <summary>Obtém os jogadores associados à conta.</summary>
    public ICollection<Jogador> Jogadores { get; set; } = [];
    /// <summary>Obtém os acessos reutilizáveis e revogáveis emitidos para a conta.</summary>
    public ICollection<TokenAcesso> TokensAcesso { get; set; } = [];

    /// <summary>Normaliza e valida o único domínio acadêmico autorizado.</summary>
    public static string NormalizarEmailFiap(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("O e-mail é obrigatório.", nameof(email));

        var normalizado = email.Trim().ToLowerInvariant();
        if (!normalizado.EndsWith("@fiap.com.br", StringComparison.Ordinal))
            throw new ArgumentException("Somente e-mails do domínio @fiap.com.br são elegíveis.", nameof(email));
        return normalizado;
    }
}
