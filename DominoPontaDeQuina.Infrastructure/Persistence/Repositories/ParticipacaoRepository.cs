using DominoPontaDeQuina.Domain.Repositories;
using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Infrastructure.Persistence.Repositories;

/// <summary>Implementa a persistência de participações com o <see cref="DominoDbContext"/>.</summary>
public sealed class ParticipacaoRepository : EfRepository<ParticipacaoPartida>, IParticipacaoRepository
{
    readonly DominoDbContext db;

    /// <summary>Inicializa o repositório de participações.</summary>
    public ParticipacaoRepository(DominoDbContext db) : base(db) => this.db = db;
    /// <inheritdoc />
    public async Task AdicionarNaPartidaAsync(ParticipacaoPartida participacao, Guid partidaId, Guid jogadorId, CancellationToken cancellationToken = default)
    {
        db.ParticipacoesPartida.Add(participacao);
        var entry = db.Entry(participacao);
        entry.Property<Guid>("PartidaId").CurrentValue = partidaId;
        entry.Property<Guid>("JogadorId").CurrentValue = jogadorId;
        await db.SaveChangesAsync(cancellationToken);
    }
}
