using DominoPontaDeQuina.Domain.Repositories;
using DominoPontaDeQuina.Infrastructure.Persistence;
using DominoPontaDeQuina.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DominoPontaDeQuina.Infrastructure;

/// <summary>Registra a persistência e os repositórios no contêiner de DI.</summary>
public static class DependencyInjection
{
    /// <summary>Adiciona o contexto e as implementações dos repositórios.</summary>
    /// <param name="services">Contêiner de serviços.</param>
    /// <param name="configureDb">Configuração do provedor e da conexão do banco.</param>
    /// <returns>O contêiner para encadeamento.</returns>
    public static IServiceCollection AddDominoInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<DominoDbContext>(configureDb);
        services.AddScoped<IPartidaRepository, PartidaRepository>();
        services.AddScoped<IJogadorRepository, JogadorRepository>();
        services.AddScoped<IParticipacaoRepository, ParticipacaoRepository>();
        services.AddScoped<ILanceRepository, LanceRepository>();
        services.AddScoped<IRankingRepository, RankingRepository>();
        return services;
    }
}
