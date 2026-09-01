using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Infrastructure.Persistence.EntityConfigurations;

/// <summary>Configura o mapeamento da entidade <see cref="Jogador"/>.</summary>
public sealed class JogadorConfiguration : IEntityTypeConfiguration<Jogador>
{
    /// <summary>Aplica o mapeamento relacional do jogador.</summary>
    public void Configure(EntityTypeBuilder<Jogador> builder)
    {
        builder.ToTable("Jogadores");
        builder.HasKey(jogador => jogador.Id);
        builder.Property(jogador => jogador.Nome).HasMaxLength(150).IsRequired();
        builder.HasOne(jogador => jogador.Usuario).WithMany(usuario => usuario.Jogadores)
            .HasForeignKey("UsuarioId").OnDelete(DeleteBehavior.SetNull);
    }
}
