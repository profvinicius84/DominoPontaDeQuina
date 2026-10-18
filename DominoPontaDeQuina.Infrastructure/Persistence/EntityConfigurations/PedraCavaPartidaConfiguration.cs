using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Infrastructure.Persistence.EntityConfigurations;

/// <summary>Configura a cava recuperável de uma rodada.</summary>
public sealed class PedraCavaPartidaConfiguration : IEntityTypeConfiguration<PedraCavaPartida>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PedraCavaPartida> builder)
    {
        builder.ToTable("PedrasCavaPartida");
        builder.HasKey(pedra => pedra.Id);
        builder.Property(pedra => pedra.Ordem).IsRequired();
        builder.HasOne(pedra => pedra.RodadaPartida).WithMany(rodada => rodada.Cava)
            .HasForeignKey(pedra => pedra.RodadaPartidaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(pedra => new { pedra.RodadaPartidaId, pedra.Ordem }).IsUnique();
    }
}
