using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Infrastructure.Persistence.EntityConfigurations;

/// <summary>Mapeia somente o hash do segredo reutilizável até revogação.</summary>
public sealed class TokenAcessoConfiguration : IEntityTypeConfiguration<TokenAcesso>
{
    public void Configure(EntityTypeBuilder<TokenAcesso> builder)
    {
        builder.ToTable("TokensAcesso");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Hash).HasMaxLength(128).IsRequired();
        builder.Property(token => token.EmitidoEmUtc).IsRequired();
        builder.HasIndex(token => token.Hash).IsUnique();
        builder.HasIndex(token => new { token.UsuarioId, token.RevogadoEmUtc });
    }
}
