using DominoPontaDeQuina.Application.Identidade.Contracts;

namespace DominoPontaDeQuina.Infrastructure.Identity;

/// <summary>Fallback explícito: não entrega nem registra tokens sem um provedor configurado.</summary>
public sealed class EntregaTokenEmailNaoConfigurada : IEntregaTokenEmail
{
    public Task EntregarAsync(string emailNormalizado, string token, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
