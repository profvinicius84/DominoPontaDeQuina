using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Infrastructure.Persistence.EntityConfigurations;

/// <summary>Configura o mapeamento da entidade <see cref="Partida"/>.</summary>
public sealed class PartidaConfiguration : IEntityTypeConfiguration<Partida>
{
    /// <summary>Aplica o mapeamento relacional da partida.</summary>
    public void Configure(EntityTypeBuilder<Partida> builder)
    {
        builder.ToTable("Partidas");
        builder.HasKey(partida => partida.Id);
        builder.Property(partida => partida.PontuacaoAlvo).IsRequired();
        builder.Property(partida => partida.Status).HasMaxLength(40).IsRequired();
        builder.Property(partida => partida.CriadaEm).IsRequired();
        builder.Property(partida => partida.VersaoEstado).IsRequired().IsConcurrencyToken();
        builder.Property(partida => partida.EncerradaEmUtc);
        builder.HasIndex(partida => new { partida.Id, partida.VersaoEstado });
    }
}
