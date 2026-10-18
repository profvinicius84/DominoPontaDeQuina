using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Infrastructure.Persistence.EntityConfigurations;

/// <summary>Configura o estado persistido de cada rodada da partida.</summary>
public sealed class RodadaPartidaConfiguration : IEntityTypeConfiguration<RodadaPartida>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RodadaPartida> builder)
    {
        builder.ToTable("RodadasPartida");
        builder.HasKey(rodada => rodada.Id);
        builder.Property(rodada => rodada.Numero).IsRequired();
        builder.Property(rodada => rodada.Situacao).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(rodada => rodada.MotivoEncerramento).HasConversion<string>().HasMaxLength(30);
        builder.Property(rodada => rodada.IniciadaEmUtc).IsRequired();
        builder.HasOne(rodada => rodada.Partida).WithMany(partida => partida.Rodadas)
            .HasForeignKey(rodada => rodada.PartidaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(rodada => new { rodada.PartidaId, rodada.Numero }).IsUnique();
    }
}
