using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Infrastructure.Persistence.EntityConfigurations;

/// <summary>Configura o mapeamento da entidade <see cref="Usuario"/>.</summary>
public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    /// <summary>Aplica o mapeamento relacional do usuário.</summary>
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(usuario => usuario.Id);
        builder.Property(usuario => usuario.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(usuario => usuario.Email).IsUnique();
        builder.Property(usuario => usuario.SenhaHash).HasMaxLength(500).IsRequired();
        builder.Property(usuario => usuario.CriadoEm).IsRequired();
    }
}
