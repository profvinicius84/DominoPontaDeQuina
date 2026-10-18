using System.Security.Cryptography;
using System.Text;
using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;

namespace DominoPontaDeQuina.Application.Jogadores;

public sealed class PerfilJogadorService(IPerfilJogadorRepository perfis, IIdentidadeRepository identidade) : IPerfilJogadorService
{
    public async Task<IReadOnlyList<PerfilJogador>> ListarAsync(Guid usuarioId, CancellationToken ct = default) =>
        (await perfis.ListarAsync(usuarioId, ct)).Select(Projetar).ToArray();

    public async Task<ResultadoPerfil<PerfilJogador>> CriarAsync(Guid usuarioId, string? nome, CancellationToken ct = default)
    {
        nome = nome?.Trim();
        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 150 || nome.Any(char.IsControl))
            return new(CodigoPerfil.EntradaInvalida);
        if (await identidade.ObterPorIdAsync(usuarioId, ct) is null)
            return new(CodigoPerfil.SemPermissao);
        return new(CodigoPerfil.Sucesso, Projetar(await perfis.CriarAsync(usuarioId, nome, ct)));
    }

    public async Task<ResultadoPerfil<PerfilJogador>> AtivarAsync(Guid usuarioId, Guid jogadorId, string? chaveAtivacao, CancellationToken ct = default)
    {
        if (jogadorId == Guid.Empty || !ChaveValida(chaveAtivacao)) return new(CodigoPerfil.EntradaInvalida);
        if (await perfis.ObterAsync(usuarioId, jogadorId, ct) is null) return new(CodigoPerfil.SemPermissao);
        if (!await perfis.TentarAtivarAsync(usuarioId, jogadorId, CalcularHash(chaveAtivacao!), DateTime.UtcNow, ct))
            return new(CodigoPerfil.JogadorJaAtivo);
        return new(CodigoPerfil.Sucesso, Projetar((await perfis.ObterAsync(usuarioId, jogadorId, ct))!));
    }

    public async Task<ResultadoPerfil<PerfilJogador>> LiberarAsync(Guid usuarioId, Guid jogadorId, string? chaveAtivacao, CancellationToken ct = default)
    {
        if (jogadorId == Guid.Empty || !ChaveValida(chaveAtivacao)) return new(CodigoPerfil.EntradaInvalida);
        var liberado = await perfis.TentarLiberarAsync(usuarioId, jogadorId, CalcularHash(chaveAtivacao!), ct);
        if (!liberado) return new(CodigoPerfil.SemPermissao);
        return new(CodigoPerfil.Sucesso, Projetar((await perfis.ObterAsync(usuarioId, jogadorId, ct))!));
    }

    public static bool ChaveValida(string? chave) => chave is { Length: 64 } && chave.All(Uri.IsHexDigit);
    public static string CalcularHash(string chave) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chave)));
    private static PerfilJogador Projetar(Jogador jogador) => new(jogador.Id, jogador.Nome, jogador.AtivacaoHash is not null, jogador.AtivadoEmUtc);
}
