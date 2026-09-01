using DominoPontaDeQuina.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DominoPontaDeQuina.Application;

/// <summary>Registra os serviços da camada de aplicação no contêiner de DI.</summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de aplicação.</summary>
    /// <param name="services">Contêiner de serviços.</param>
    /// <returns>O contêiner para encadeamento.</returns>
    public static IServiceCollection AddDominoApplication(this IServiceCollection services)
    {
        services.AddScoped<IPartidaService, PartidaService>();
        return services;
    }
}
