namespace DominoPontaDeQuina.Application.Identidade.Contracts;

/// <summary>Adaptador de entrega de segredo; implementações não devem registrá-lo em logs.</summary>
public interface IEntregaTokenEmail
{
    Task EntregarAsync(string emailNormalizado, string token, CancellationToken cancellationToken = default);
}
