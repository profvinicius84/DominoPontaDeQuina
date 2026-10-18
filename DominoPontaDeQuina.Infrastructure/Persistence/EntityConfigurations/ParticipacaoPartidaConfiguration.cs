using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Infrastructure.Persistence.EntityConfigurations;

/// <summary>Configura o mapeamento de uma participação em partida.</summary>
public sealed class ParticipacaoPartidaConfiguration : IEntityTypeConfiguration<ParticipacaoPartida>
{
    /// <summary>Aplica chave composta, FKs implícitas e regras de exclusão.</summary>
    public void Configure(EntityTypeBuilder<ParticipacaoPartida> builder)
    {
        builder.ToTable("ParticipacoesPartida");
        builder.HasKey(participacao => new { participacao.PartidaId, participacao.JogadorId });
        builder.Property(participacao => participacao.Id).IsRequired();
        builder.Property(participacao => participacao.Posicao).IsRequired();
        builder.Property(participacao => participacao.WebhookUrl).HasMaxLength(2048);
        builder.Property(participacao => participacao.Pontos).IsRequired();
        builder.Property(participacao => participacao.QuantidadePedrasNaMao).IsRequired();
        builder.HasOne(participacao => participacao.Partida).WithMany(partida => partida.Participacoes)
            .HasForeignKey(participacao => participacao.PartidaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(participacao => participacao.Jogador).WithMany(jogador => jogador.Participacoes)
            .HasForeignKey(participacao => participacao.JogadorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(participacao => new { participacao.PartidaId, participacao.Posicao }).IsUnique();
        builder.HasIndex(participacao => participacao.Id).IsUnique();
    }
}
