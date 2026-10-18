using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DominoPontaDeQuina.Application.Identidade.Contracts;
using Microsoft.Extensions.Options;

namespace DominoPontaDeQuina.Infrastructure.Identity;

public sealed class EntregaTokenEmailSendGridOptions
{
    public bool Habilitado { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string Remetente { get; init; } = string.Empty;
}

/// <summary>Entrega o token por meio da API Mail Send, sem registrar a credencial.</summary>
public sealed class EntregaTokenEmailSendGrid(HttpClient cliente, IOptions<EntregaTokenEmailSendGridOptions> options) : IEntregaTokenEmail
{
    public async Task EntregarAsync(string emailNormalizado, string token, CancellationToken cancellationToken = default)
    {
        var configuracao = options.Value;
        if (string.IsNullOrWhiteSpace(configuracao.ApiKey) || string.IsNullOrWhiteSpace(configuracao.Remetente))
            throw new InvalidOperationException("O SendGrid requer chave de API e remetente configurados.");

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuracao.ApiKey);
        requisicao.Content = JsonContent.Create(new
        {
            personalizations = new[] { new { to = new[] { new { email = emailNormalizado } } } },
            from = new { email = configuracao.Remetente },
            subject = "Seu acesso ao Dominó Ponta de Quina",
            content = new[] { new { type = "text/plain", value = string.Join("\n",
                "Olá!",
                "",
                "Seu acesso ao Dominó Ponta de Quina está pronto.",
                "",
                "Seu token de acesso:",
                token,
                "",
                "Copie o token e cole no campo 'Token recebido por e-mail' do aplicativo. Você pode usá-lo novamente em outros acessos, até revogá-lo.",
                "Guarde-o com cuidado e não o compartilhe com outras pessoas.",
                "",
                "Se você não solicitou este acesso, ignore esta mensagem.",
                "",
                "Bom jogo!") } }
        });

        using var resposta = await cliente.SendAsync(requisicao, cancellationToken);
        if (resposta.StatusCode != HttpStatusCode.Accepted)
            throw new HttpRequestException("O SendGrid não aceitou a entrega do e-mail.");
    }
}
