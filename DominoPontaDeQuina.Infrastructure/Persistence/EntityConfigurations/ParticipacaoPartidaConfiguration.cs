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
        builder.Property<Guid>("PartidaId");
        builder.Property<Guid>("JogadorId");
        builder.HasKey("PartidaId", "JogadorId");
        builder.Property(participacao => participacao.Pontos).IsRequired();
        builder.HasOne(participacao => participacao.Partida).WithMany(partida => partida.Participacoes)
            .HasForeignKey("PartidaId").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(participacao => participacao.Jogador).WithMany(jogador => jogador.Participacoes)
            .HasForeignKey("JogadorId").OnDelete(DeleteBehavior.Cascade);
    }
}
