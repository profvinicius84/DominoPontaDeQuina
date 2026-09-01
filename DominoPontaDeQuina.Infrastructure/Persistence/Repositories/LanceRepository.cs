using DominoPontaDeQuina.Domain.Repositories;
using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Infrastructure.Persistence.Repositories;

/// <summary>Implementa a persistência de lances com o <see cref="DominoDbContext"/>.</summary>
public sealed class LanceRepository : EfRepository<Lance>, ILanceRepository
{
    readonly DominoDbContext db;

    /// <summary>Inicializa o repositório de lances.</summary>
    public LanceRepository(DominoDbContext db) : base(db) => this.db = db;
    /// <inheritdoc />
    public async Task<Lance> AdicionarNaPartidaAsync(Lance lance, Guid partidaId, Guid jogadorId, CancellationToken cancellationToken = default)
    {
        db.Lances.Add(lance);
        var entry = db.Entry(lance);
        entry.Property<Guid>("PartidaId").CurrentValue = partidaId;
        entry.Property<Guid>("JogadorId").CurrentValue = jogadorId;
        await db.SaveChangesAsync(cancellationToken);
        return lance;
    }
}
