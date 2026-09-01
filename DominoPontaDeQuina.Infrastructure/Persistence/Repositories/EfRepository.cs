using DominoPontaDeQuina.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Infrastructure.Persistence.Repositories;

/// <summary>Implementa o CRUD comum dos repositórios usando EF Core.</summary>
/// <typeparam name="TEntity">Tipo da entidade persistida.</typeparam>
public abstract class EfRepository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    /// <summary>Obtém o contexto utilizado pelo repositório.</summary>
    protected DominoDbContext Db { get; }

    /// <summary>Inicializa um repositório EF Core.</summary>
    /// <param name="db">Contexto do banco de dados.</param>
    protected EfRepository(DominoDbContext db) => Db = db;

    /// <inheritdoc />
    public virtual async Task<TEntity?> ObterPorIdAsync(object[] keys, CancellationToken cancellationToken = default) =>
        await Db.Set<TEntity>().FindAsync(keys, cancellationToken);

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> ObterTodosAsync(CancellationToken cancellationToken = default) =>
        await Db.Set<TEntity>().AsNoTracking().ToListAsync(cancellationToken);

    /// <inheritdoc />
    public virtual async Task<TEntity> AdicionarAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Db.Set<TEntity>().Add(entity);
        await Db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> AtualizarAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Db.Set<TEntity>().Update(entity);
        await Db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public virtual async Task RemoverAsync(object[] keys, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<TEntity>().FindAsync(keys, cancellationToken);
        if (entity is null)
            return;

        Db.Set<TEntity>().Remove(entity);
        await Db.SaveChangesAsync(cancellationToken);
    }
}
