using System.Security.Cryptography;
using System.Text;
using DominoPontaDeQuina.Application.Identidade.Contracts;
using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;

namespace DominoPontaDeQuina.Application.Identidade;

/// <summary>Autenticação acadêmica restrita à FIAP e gestão segura do ciclo de token.</summary>
public sealed class IdentidadeAcessoService(IIdentidadeRepository identidade, IEntregaTokenEmail entrega, IJogadorRepository? jogadores = null) : IIdentidadeAcessoService
{
    // Decisão explícita do responsável para o ambiente acadêmico atual.
    private const string SenhaFixa = "3ESC#-CP6";

    public async Task<ResultadoCadastroUsuario> CadastrarUsuarioAutorizadoAsync(string email, CancellationToken cancellationToken = default)
    {
        string normalizado;
        try { normalizado = Usuario.NormalizarEmailFiap(email); }
        catch (ArgumentException) { return new(false, null); }
        var existente = await identidade.ObterPorEmailAsync(normalizado, cancellationToken);
        if (existente is not null) return new(true, existente.Id);
        var cadastro = await identidade.AdicionarUsuarioAsync(new Usuario
        {
            Email = normalizado,
            // A senha é validada pela política fixa acima; o valor persistido não é um segredo reutilizável.
            SenhaHash = "POLITICA_SENHA_FIXA_ACADEMICA"
        }, cancellationToken);
        if (cadastro.Adicionado && jogadores is not null)
        {
            await jogadores.AdicionarComUsuarioAsync(new Jogador
            {
                Nome = normalizado[..normalizado.IndexOf('@')]
            }, cadastro.Usuario.Id, cancellationToken);
        }
        return new(true, cadastro.Usuario.Id);
    }

    public async Task<ResultadoAutenticacao> AutenticarAsync(string email, string senha, CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioElegivelAsync(email, senha, cancellationToken);
        return usuario is null ? new(false, null) : new(true, usuario.Id);
    }

    public async Task<SolicitacaoTokenRecebida> SolicitarTokenAsync(string email, string senha, CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioElegivelAsync(email, senha, cancellationToken);
        if (usuario is null)
            return new();

        var token = GerarToken();
        var agora = DateTime.UtcNow;
        await identidade.EmitirTokenAsync(new TokenAcesso
        {
            UsuarioId = usuario.Id,
            Hash = CalcularHash(token),
            EmitidoEmUtc = agora
        }, cancellationToken);
        await entrega.EntregarAsync(usuario.Email, token, cancellationToken);
        return new();
    }

    public async Task<SolicitacaoTokenRecebida> SolicitarTokenPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var cadastro = await CadastrarUsuarioAutorizadoAsync(email, cancellationToken);
        return cadastro.Sucesso
            ? await SolicitarTokenAsync(email, SenhaFixa, cancellationToken)
            : new();
    }

    public async Task<Guid?> ValidarTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var acesso = await identidade.ObterTokenValidoPorHashAsync(CalcularHash(token), cancellationToken);
        return acesso?.UsuarioId;
    }

    public Task<bool> RevogarTokenAsync(Guid usuarioId, string token, CancellationToken cancellationToken = default) =>
        usuarioId == Guid.Empty || string.IsNullOrWhiteSpace(token)
            ? Task.FromResult(false)
            : identidade.RevogarTokenAsync(usuarioId, CalcularHash(token), DateTime.UtcNow, cancellationToken);

    private async Task<Usuario?> ObterUsuarioElegivelAsync(string email, string senha, CancellationToken cancellationToken)
    {
        if (!string.Equals(senha, SenhaFixa, StringComparison.Ordinal)) return null;
        try { return await identidade.ObterPorEmailAsync(Usuario.NormalizarEmailFiap(email), cancellationToken); }
        catch (ArgumentException) { return null; }
    }

    private static string GerarToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private static string CalcularHash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
