using Bunit;
using DominoPontaDeQuina.Blazor.Pages;
using DominoPontaDeQuina.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace DominoPontaDeQuina.Tests;

public sealed class PainelTests : TestContext
{
    [Fact]
    public void PainelComecaSemPartidaESemMaoPrivada()
    {
        Configurar(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var painel = RenderComponent<Home>();
        Assert.Contains("Nenhuma partida selecionada", painel.Markup);
        Assert.Equal("password", painel.Find("#token").GetAttribute("type"));
        Assert.Empty(painel.FindAll(".tile"));
    }

    [Fact]
    public void PainelExibeSomenteMesaContagensEEncerramento()
    {
        Configurar(_ => Task.FromResult(Resposta(SituacaoPartida.Encerrada)));
        var painel = RenderComponent<Home>();
        Conectar(painel);
        painel.WaitForAssertion(() => Assert.Contains("Partida encerrada.", painel.Markup));
        Assert.Contains("7 pedras", painel.Markup);
        Assert.Single(painel.FindAll(".tile"));
        Assert.DoesNotContain("minhaMao", painel.Markup);
        Assert.DoesNotContain("pedra-privada", painel.Markup);
    }

    [Fact]
    public void FalhaPosteriorPreservaUltimaVisaoComAvisoDesatualizado()
    {
        var contador = 0;
        Configurar(_ => Task.FromResult(++contador == 1 ? Resposta() : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var painel = RenderComponent<Home>();
        Conectar(painel);
        painel.WaitForAssertion(() => Assert.Single(painel.FindAll(".tile")));
        painel.FindAll("button").Single(b => b.TextContent == "Atualizar agora").Click();
        painel.WaitForAssertion(() => Assert.Contains("desatualizados", painel.Find("[role=status]").TextContent));
        Assert.Single(painel.FindAll(".tile"));
    }

    [Theory]
    [InlineData(401, "Token ausente, inválido ou revogado")]
    [InlineData(404, "Partida não encontrada")]
    public void AusenciaOuAcessoInvalidoNaoMantemDadosAnteriores(int status, string mensagem)
    {
        Configurar(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)));
        var painel = RenderComponent<Home>();
        Conectar(painel);
        painel.WaitForAssertion(() => Assert.Contains(mensagem, painel.Markup));
        Assert.Empty(painel.FindAll(".tile"));
    }

    [Fact]
    public void CarregamentoImpedeConsultasSobrepostas()
    {
        var resposta = new TaskCompletionSource<HttpResponseMessage>();
        Configurar(_ => resposta.Task);
        var painel = RenderComponent<Home>();
        Conectar(painel);
        painel.WaitForAssertion(() => Assert.Contains("Consultando partida", painel.Markup));
        Assert.True(painel.FindAll("button").Single(b => b.TextContent == "Atualizar agora").HasAttribute("disabled"));
        resposta.SetResult(Resposta());
        painel.WaitForAssertion(() => Assert.Contains("Partida atualizada", painel.Markup));
    }

    private void Configurar(Func<HttpRequestMessage, Task<HttpResponseMessage>> resposta) =>
        Services.AddSingleton(new DominoApiClient(new HttpClient(new ClientTests.Handler(resposta)) { BaseAddress = new Uri("http://localhost") }));

    [Fact]
    public void AtualizacaoAntigaNaoSubstituiVersaoMaisNova()
    {
        var contador = 0;
        Configurar(request =>
        {
            Assert.EndsWith("/visualizar", request.RequestUri!.AbsolutePath);
            Assert.False(request.Headers.Contains("X-Jogador-Ativacao"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new
            {
                partidaId = Guid.NewGuid(), versaoEstado = ++contador == 1 ? 10 : 2,
                situacao = 1, numeroRodada = 1, tabuleiro = Array.Empty<Pedra>(), participantes = Array.Empty<Participante>(),
                pedraAberturaObrigatoria = "segredo-indevido", minhaMao = "segredo-indevido"
            }) });
        });
        var painel = RenderComponent<Home>(); Conectar(painel);
        painel.WaitForAssertion(() => Assert.Contains("Versão 10", painel.Markup));
        painel.FindAll("button").Single(b => b.TextContent == "Atualizar agora").Click();
        painel.WaitForAssertion(() => Assert.Contains("Versão 10", painel.Markup));
        Assert.DoesNotContain("segredo-indevido", painel.Markup);
    }
    private static void Conectar(IRenderedComponent<Home> painel)
    {
        painel.Find("#token").Change("token-ficticio");
        painel.Find("#partida").Change(Guid.NewGuid().ToString());
        painel.FindAll("button").Single(b => b.TextContent == "Acompanhar").Click();
    }
    private static HttpResponseMessage Resposta(SituacaoPartida situacao = SituacaoPartida.EmAndamento) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(new
        {
            partidaId = Guid.NewGuid(), versaoEstado = 2, situacao, numeroRodada = 1,
            tabuleiro = new[] { new Pedra(6, 5) },
            participantes = new[] { new Participante(Guid.NewGuid(), "Jogador", 1, 7, true) },
            minhaMao = new[] { "pedra-privada" }
        }) };
}
