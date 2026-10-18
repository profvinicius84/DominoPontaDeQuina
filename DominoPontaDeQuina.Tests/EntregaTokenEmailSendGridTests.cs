using System.Net;
using System.Text.Json;
using DominoPontaDeQuina.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace DominoPontaDeQuina.Tests;

public sealed class EntregaTokenEmailSendGridTests
{
    [Fact]
    public async Task EnviaTokenAoDestinatarioAutorizadoPelaApiMailSend()
    {
        using var cliente = new HttpClient(new Resposta(async requisicao =>
        {
            Assert.Equal(HttpMethod.Post, requisicao.Method);
            Assert.Equal("https://api.sendgrid.com/v3/mail/send", requisicao.RequestUri!.ToString());
            Assert.Equal("Bearer", requisicao.Headers.Authorization!.Scheme);
            Assert.Equal("chave-teste", requisicao.Headers.Authorization.Parameter);
            using var corpo = JsonDocument.Parse(await requisicao.Content!.ReadAsStringAsync());
            var raiz = corpo.RootElement;
            Assert.Equal("destino@fiap.com.br", raiz.GetProperty("personalizations")[0].GetProperty("to")[0].GetProperty("email").GetString());
            Assert.Equal("remetente@exemplo.com", raiz.GetProperty("from").GetProperty("email").GetString());
            Assert.Equal("text/plain", raiz.GetProperty("content")[0].GetProperty("type").GetString());
            Assert.Equal("Seu acesso ao Dominó Ponta de Quina", raiz.GetProperty("subject").GetString());
            var mensagem = raiz.GetProperty("content")[0].GetProperty("value").GetString()!;
            Assert.Contains("TOKEN-TESTE", mensagem);
            Assert.Contains("Token recebido por e-mail", mensagem);
            Assert.Contains("não o compartilhe", mensagem);
            Assert.DoesNotContain("Authorization", mensagem);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }));
        var entrega = new EntregaTokenEmailSendGrid(cliente, Opcoes());

        await entrega.EntregarAsync("destino@fiap.com.br", "TOKEN-TESTE");
    }

    [Fact]
    public async Task RejeicaoNaoExpoeTokenNemRespostaDoProvedor()
    {
        using var cliente = new HttpClient(new Resposta(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        { Content = new StringContent("TOKEN-TESTE detalhe interno") })));
        var entrega = new EntregaTokenEmailSendGrid(cliente, Opcoes());

        var erro = await Assert.ThrowsAsync<HttpRequestException>(() => entrega.EntregarAsync("destino@fiap.com.br", "TOKEN-TESTE"));
        Assert.DoesNotContain("TOKEN-TESTE", erro.Message);
        Assert.DoesNotContain("detalhe interno", erro.Message);
    }

    private static IOptions<EntregaTokenEmailSendGridOptions> Opcoes() => Options.Create(new EntregaTokenEmailSendGridOptions
    {
        Habilitado = true,
        ApiKey = "chave-teste",
        Remetente = "remetente@exemplo.com"
    });

    private sealed class Resposta(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);
    }
}
