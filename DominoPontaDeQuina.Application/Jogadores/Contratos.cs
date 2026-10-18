namespace DominoPontaDeQuina.Application.Jogadores;

public sealed record PerfilJogador(Guid JogadorId, string Nome, bool Ativo, DateTime? AtivadoEmUtc);
public enum CodigoPerfil { Sucesso, EntradaInvalida, SemPermissao, JogadorJaAtivo }
public sealed record ResultadoPerfil<T>(CodigoPerfil Codigo, T? Valor = default);

/// <summary>Prova de posse da ativação. Não representa um objeto de participação.</summary>
public sealed class AcessoJogador(Guid jogadorId, string? chaveAtivacao)
{
    public Guid JogadorId { get; } = jogadorId;
    public string? ChaveAtivacao { get; } = chaveAtivacao;
}

public interface IPerfilJogadorService
{
    Task<IReadOnlyList<PerfilJogador>> ListarAsync(Guid usuarioId, CancellationToken ct = default);
    Task<ResultadoPerfil<PerfilJogador>> CriarAsync(Guid usuarioId, string? nome, CancellationToken ct = default);
    Task<ResultadoPerfil<PerfilJogador>> AtivarAsync(Guid usuarioId, Guid jogadorId, string? chaveAtivacao, CancellationToken ct = default);
    Task<ResultadoPerfil<PerfilJogador>> LiberarAsync(Guid usuarioId, Guid jogadorId, string? chaveAtivacao, CancellationToken ct = default);
}
