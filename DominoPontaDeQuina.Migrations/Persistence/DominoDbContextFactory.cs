using DominoPontaDeQuina.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DominoPontaDeQuina.Migrations.Persistence;

/// <summary>Cria o contexto em design-time para comandos de migrations.</summary>
public sealed class DominoDbContextFactory : IDesignTimeDbContextFactory<DominoDbContext>
{
    /// <summary>Cria um <see cref="DominoDbContext"/> configurado para SQL Server LocalDB.</summary>
    /// <param name="args">Argumentos recebidos pela ferramenta do EF Core.</param>
    /// <returns>Contexto configurado para design-time.</returns>
    public DominoDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<DominoDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), options =>
            options.MigrationsAssembly(typeof(DominoDbContextFactory).Assembly.FullName));

        return new DominoDbContext(optionsBuilder.Options);
    }
}
