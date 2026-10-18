using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Infrastructure.Persistence.EntityConfigurations;

public sealed class EventoWebhookConfiguration : IEntityTypeConfiguration<EventoWebhook>
{
    public void Configure(EntityTypeBuilder<EventoWebhook> builder)
    {
        builder.ToTable("EventosWebhook");
        builder.HasKey(evento => evento.Id);
        builder.Property(evento => evento.Tipo).HasMaxLength(80).IsRequired();
        builder.Property(evento => evento.Destino).HasMaxLength(2048).IsRequired();
        builder.Property(evento => evento.ConteudoJson).IsRequired();
        builder.Property(evento => evento.UltimaFalha).HasMaxLength(200);
        builder.HasIndex(evento => new { evento.PartidaId, evento.JogadorId, evento.VersaoEstado }).IsUnique();
        builder.HasIndex(evento => new { evento.EntregueEmUtc, evento.AbandonadoEmUtc, evento.ProximaTentativaEmUtc });
        builder.HasOne<Partida>().WithMany().HasForeignKey(evento => evento.PartidaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Jogador>().WithMany().HasForeignKey(evento => evento.JogadorId).OnDelete(DeleteBehavior.Restrict);
    }
}
