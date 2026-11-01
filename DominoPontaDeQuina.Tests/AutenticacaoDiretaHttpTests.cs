using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DominoPontaDeQuina.Application.Identidade.Contracts;
using DominoPontaDeQuina.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DominoPontaDeQuina.Tests;

public sealed class AutenticacaoDiretaHttpTests
{
    [Fact]
    public async Task Token_RejeitaEmailInvalidoELimitaSolicitacoes()
    {
        using var host = new ApiHttpIntegrationTests.DominoApiFactory();
        using var http = host.CreateClient();

        foreach (var email in new string?[] { null, "", "@fiap.com.br", "aluno@example.com" })
        {
            using var resposta = await http.PostAsJsonAsync("/api/identidade/token", new { email });
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("false", await resposta.Content.ReadAsStringAsync());
        }

        for (var tentativa = 0; tentativa < 5; tentativa++)
        {
            var email = tentativa == 0 ? "ALUNO@FIAP.COM.BR" : "aluno@fiap.com.br";
            using var resposta = await http.PostAsJsonAsync("/api/identidade/token", new { email });
            Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
        }

        using var bloqueada = await http.PostAsJsonAsync("/api/identidade/token", new { email = "aluno@fiap.com.br" });
        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
        Assert.Equal("LIMITE_EXCEDIDO", (await bloqueada.Content.ReadFromJsonAsync<RespostaErro>())!.Codigo);
        using var outroAluno = await http.PostAsJsonAsync("/api/identidade/token", new { email = "outro@fiap.com.br" });
        Assert.Equal(HttpStatusCode.Accepted, outroAluno.StatusCode);
        using var scope = host.Services.CreateScope();
        Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<DominoDbContext>().Usuarios.CountAsync());
    }

    [Fact]
    public async Task Token_LimitaCinquentaSolicitacoesPorIpMesmoComEmailsDiferentes()
    {
        using var host = new ApiHttpIntegrationTests.DominoApiFactory();
        using var http = host.CreateClient();

        for (var tentativa = 0; tentativa < 50; tentativa++)
        {
            using var resposta = await http.PostAsJsonAsync("/api/identidade/token", new { email = $"invalido-{tentativa}" });
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }

        using var bloqueada = await http.PostAsJsonAsync("/api/identidade/token", new { email = "aluno@fiap.com.br" });
        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
        Assert.Equal("LIMITE_EXCEDIDO", (await bloqueada.Content.ReadFromJsonAsync<RespostaErro>())!.Codigo);
    }

    [Fact]
    public async Task Login_NaoExiste()
    {
        using var host = new ApiHttpIntegrationTests.DominoApiFactory();
        using var http = host.CreateClient();
        using var resposta = await http.PostAsJsonAsync("/api/identidade/login",
            new { email = "login@fiap.com.br", senha = "3ESC#-CP6" });
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task TokenSomenteEmail_EmiteCredenciaisIndependentesAteRevogacao()
    {
        using var host = new ApiHttpIntegrationTests.DominoApiFactory();
        using var http = host.CreateClient();
        using var resposta = await http.PostAsJsonAsync("/api/identidade/token", new { email = "login@fiap.com.br" });
        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
        Assert.Equal("", await resposta.Content.ReadAsStringAsync());
        var primeiroToken = host.Entrega.ObterToken("login@fiap.com.br");
        Assert.Equal(HttpStatusCode.Accepted, (await http.PostAsJsonAsync("/api/identidade/token", new { email = "login@fiap.com.br" })).StatusCode);
        using var desconhecido = await http.PostAsJsonAsync("/api/identidade/token", new { email = "naoexiste@fiap.com.br" });
        Assert.Equal(HttpStatusCode.Accepted, desconhecido.StatusCode); Assert.Equal("", await desconhecido.Content.ReadAsStringAsync());
        var tokenNovo = host.Entrega.ObterToken("naoexiste@fiap.com.br");
        Assert.NotEmpty(tokenNovo);
        using (var scope = host.Services.CreateScope())
            Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<DominoDbContext>().Usuarios.CountAsync());
        var email = host.Entrega.ObterToken("login@fiap.com.br"); Assert.NotEqual(email, primeiroToken);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", primeiroToken);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/identidade/sessao")).StatusCode);
        await http.PostAsync("/api/identidade/tokens/revogacao", null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/identidade/sessao")).StatusCode);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", email);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/identidade/sessao")).StatusCode);
    }
}
