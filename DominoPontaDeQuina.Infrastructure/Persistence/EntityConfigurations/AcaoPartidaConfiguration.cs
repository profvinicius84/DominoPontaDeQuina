using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Infrastructure.Persistence.EntityConfigurations;

/// <summary>Configura histórico, idempotência e ordenação de tentativas de ação.</summary>
public sealed class AcaoPartidaConfiguration : IEntityTypeConfiguration<AcaoPartida>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AcaoPartida> builder)
    {
        builder.ToTable("AcoesPartida");
        builder.HasKey(acao => acao.Id);
        builder.Property(acao => acao.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(acao => acao.Lado).HasConversion<string>().HasMaxLength(15);
        builder.Property(acao => acao.ChaveIdempotencia).IsRequired();
        builder.Property(acao => acao.RecebidaEmUtc).IsRequired();
        builder.Property(acao => acao.OrdemRecebimento).IsRequired();
        builder.Property(acao => acao.FoiAceita).IsRequired();
        builder.HasOne(acao => acao.Partida).WithMany(partida => partida.Acoes)
            .HasForeignKey(acao => acao.PartidaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(acao => acao.RodadaPartida).WithMany(rodada => rodada.Acoes)
            .HasForeignKey(acao => acao.RodadaPartidaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(acao => acao.Autor).WithMany()
            .HasForeignKey(acao => acao.AutorJogadorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(acao => new { acao.PartidaId, acao.AutorJogadorId, acao.ChaveIdempotencia }).IsUnique();
        builder.HasIndex(acao => new { acao.PartidaId, acao.OrdemRecebimento }).IsUnique();
        builder.HasIndex(acao => new { acao.PartidaId, acao.RecebidaEmUtc, acao.OrdemRecebimento });
    }
}
