namespace DominoPontaDeQuina.Application.Identidade.Contracts;

/// <summary>Operações internas de identidade, sem contratos HTTP e sem expor tokens.</summary>
public interface IIdentidadeAcessoService
{
    Task<ResultadoCadastroUsuario> CadastrarUsuarioAutorizadoAsync(string email, CancellationToken cancellationToken = default);
    Task<ResultadoAutenticacao> AutenticarAsync(string email, string senha, CancellationToken cancellationToken = default);
    Task<SolicitacaoTokenRecebida> SolicitarTokenPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<SolicitacaoTokenRecebida> SolicitarTokenAsync(string email, string senha, CancellationToken cancellationToken = default);
    Task<Guid?> ValidarTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> RevogarTokenAsync(Guid usuarioId, string token, CancellationToken cancellationToken = default);
}

public sealed record ResultadoCadastroUsuario(bool Sucesso, Guid? UsuarioId);
public sealed record ResultadoAutenticacao(bool Sucesso, Guid? UsuarioId);
/// <summary>Confirmação uniforme: não revela elegibilidade, token nem situação da entrega.</summary>
public sealed record SolicitacaoTokenRecebida();
