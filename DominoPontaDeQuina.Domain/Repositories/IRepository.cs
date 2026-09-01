namespace DominoPontaDeQuina.Domain.Repositories;

/// <summary>Define as operações CRUD comuns para uma entidade persistida.</summary>
/// <typeparam name="TEntity">Tipo da entidade manipulada.</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>Obtém uma entidade pela chave primária.</summary>
    /// <param name="keys">Valores da chave, na ordem configurada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A entidade ou <see langword="null"/> se não existir.</returns>
    Task<TEntity?> ObterPorIdAsync(object[] keys, CancellationToken cancellationToken = default);

    /// <summary>Obtém todas as entidades.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Entidades persistidas.</returns>
    Task<IReadOnlyList<TEntity>> ObterTodosAsync(CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma entidade.</summary>
    /// <param name="entity">Entidade a ser adicionada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A entidade persistida.</returns>
    Task<TEntity> AdicionarAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Atualiza uma entidade.</summary>
    /// <param name="entity">Entidade a ser atualizada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A entidade atualizada.</returns>
    Task<TEntity> AtualizarAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Remove uma entidade pela chave primária.</summary>
    /// <param name="keys">Valores da chave, na ordem configurada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task RemoverAsync(object[] keys, CancellationToken cancellationToken = default);
}
