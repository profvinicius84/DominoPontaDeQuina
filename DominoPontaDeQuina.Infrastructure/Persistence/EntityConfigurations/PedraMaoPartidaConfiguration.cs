using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Infrastructure.Persistence.EntityConfigurations;

/// <summary>Configura as pedras privadas da mão de cada participante.</summary>
public sealed class PedraMaoPartidaConfiguration : IEntityTypeConfiguration<PedraMaoPartida>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PedraMaoPartida> builder)
    {
        builder.ToTable("PedrasMaoPartida");
        builder.HasKey(pedra => pedra.Id);
        builder.Property(pedra => pedra.ValorA).IsRequired();
        builder.Property(pedra => pedra.ValorB).IsRequired();
        builder.HasOne(pedra => pedra.Participacao).WithMany(participacao => participacao.Mao)
            .HasForeignKey(pedra => new { pedra.PartidaId, pedra.JogadorId })
            .HasPrincipalKey(participacao => new { participacao.PartidaId, participacao.JogadorId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(pedra => new { pedra.PartidaId, pedra.JogadorId, pedra.ValorA, pedra.ValorB }).IsUnique();
    }
}
