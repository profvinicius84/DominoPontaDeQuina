using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DominoPontaDeQuina.Client;
using DominoPontaDeQuina.Application.Identidade.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DominoPontaDeQuina.Tests;

public class ClientTests
{
    [Fact]
    public async Task DoisClientesCompartilhamTokenERecebemTurnoPorConsulta()
    {
        using var host = new ApiHttpIntegrationTests.DominoApiFactory();
        using (var scope = host.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IIdentidadeAcessoService>()
                .CadastrarUsuarioAutorizadoAsync("cliente-polling@fiap.com.br");
        using var httpA = host.CreateClient();
        using var httpB = host.CreateClient();
        var a = new DominoApiClient(httpA);
        var b = new DominoApiClient(httpB);
        await a.SolicitarTokenAsync("cliente-polling@fiap.com.br");
        var token = host.Entrega.ObterToken("cliente-polling@fiap.com.br");
        var perfilA = Assert.Single(await a.ListarJogadoresAsync(token));
        var perfilB = await b.CriarJogadorAsync(token, "Segundo jogador");
        foreach (var (cliente, perfil) in new[] { (a, perfilA), (b, perfilB) })
        {
            var chave = DominoApiClient.GerarChaveAtivacao();
            await cliente.AtivarJogadorAsync(token, perfil.JogadorId, chave);
            cliente.SelecionarJogador(perfil.JogadorId, chave);
        }

        var criada = await a.CriarAsync(token, 1);
        Assert.Contains(await b.ListarPartidasAsync(token), p => p.PartidaId == criada.PartidaId);
        var ingresso = await b.AcaoAsync(token, criada.PartidaId, "entrar", criada.VersaoEstado, Guid.NewGuid());
        var vistaA = await a.AtualizacoesAsync(token, criada.PartidaId, criada.VersaoEstado);
        Assert.Equal(ingresso.Estado.VersaoEstado, vistaA!.VersaoEstado);
        Assert.Null(await b.AtualizacoesAsync(token, criada.PartidaId, ingresso.Estado.VersaoEstado));

        var inicio = await a.AcaoAsync(token, criada.PartidaId, "iniciar", ingresso.Estado.VersaoEstado, Guid.NewGuid());
        var vistaB = await b.AtualizacoesAsync(token, criada.PartidaId, ingresso.Estado.VersaoEstado);
        Assert.Equal(inicio.Estado.VersaoEstado, vistaB!.VersaoEstado);
        Assert.Equal(7, inicio.Estado.MinhaMao.Count);
        Assert.Equal(7, vistaB.MinhaMao.Count);
        Assert.Empty(inicio.Estado.MinhaMao.Intersect(vistaB.MinhaMao));
        Assert.Single(new[] { inicio.Estado, vistaB }, e => e.PedraAberturaObrigatoria is not null);

        var daVez = inicio.Estado.JogadorDoTurnoId == perfilA.JogadorId ? a : b;
        var estadoDaVez = daVez == a ? inicio.Estado : vistaB;
        var abertura = estadoDaVez.PedraAberturaObrigatoria!;
        var jogada = await daVez.JogarAsync(token, criada.PartidaId, abertura, LadoJogavel.Esquerda,
            estadoDaVez.VersaoEstado, Guid.NewGuid());
        Assert.Single(jogada.Estado.Tabuleiro);
        var outro = daVez == a ? b : a;
        var atualizacao = await outro.AtualizacoesAsync(token, criada.PartidaId, estadoDaVez.VersaoEstado);
        Assert.Equal(jogada.Estado.VersaoEstado, atualizacao!.VersaoEstado);
        Assert.Single(atualizacao.Tabuleiro);
    }

    [Fact]
    public async Task FluxoPorConsultaAplicaRespostaDaAcaoSemEsperarCallback()
    {
        var partidaId = Guid.NewGuid();
        var fluxo = new FluxoEstadoCliente { EsperaCallback = TimeSpan.Zero };
        var consultas = 0;
        var confirmado = Estado(partidaId);
        var recebido = await fluxo.ExecutarAsync(() => Task.FromResult(confirmado), () =>
        {
            consultas++;
            return Task.FromResult(confirmado);
        });
        Assert.Same(confirmado, recebido);
        Assert.Same(confirmado, fluxo.Estado);
        Assert.Equal(0, consultas);
    }

    [Fact]
    public async Task ConsultaAtualizacoesEnviaVersaoEProvaETrataEstadoInalterado()
    {
        var partidaId = Guid.NewGuid();
        var jogadorId = Guid.NewGuid();
        var ativacao = DominoApiClient.GerarChaveAtivacao();
        var chamadas = 0;
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal($"/api/partidas/{partidaId}/atualizacoes", request.RequestUri!.AbsolutePath);
            Assert.Contains($"jogadorId={jogadorId}", request.RequestUri.Query);
            Assert.Contains("versaoConhecida=4", request.RequestUri.Query);
            Assert.Equal("segredo", request.Headers.Authorization!.Parameter);
            Assert.Equal(ativacao, request.Headers.GetValues("X-Jogador-Ativacao").Single());
            return Task.FromResult(++chamadas == 1
                ? new HttpResponseMessage(HttpStatusCode.NoContent)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Estado(partidaId)) });
        })) { BaseAddress = new Uri("http://localhost") };
        var cliente = new DominoApiClient(http);
        cliente.SelecionarJogador(jogadorId, ativacao);
        Assert.Null(await cliente.AtualizacoesAsync("segredo", partidaId, 4));
        Assert.Equal(partidaId, (await cliente.AtualizacoesAsync("segredo", partidaId, 4))!.PartidaId);
        Assert.Equal(2, chamadas);
    }

    [Fact]
    public async Task JogadaUsaSomenteContratoPublicoEChaveFornecida()
    {
        var id = Guid.NewGuid(); var chave = Guid.NewGuid();
        var jogadorId = Guid.NewGuid();
        var ativacao = DominoApiClient.GerarChaveAtivacao();
        using var http = new HttpClient(new Handler(async request =>
        {
            Assert.Equal($"/api/partidas/{id}/jogar", request.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            Assert.Equal("teste-local", request.Headers.Authorization.Parameter);
            Assert.DoesNotContain("teste-local", request.RequestUri.ToString());
            Assert.Equal(ativacao, request.Headers.GetValues("X-Jogador-Ativacao").Single());
            Assert.DoesNotContain(ativacao, request.RequestUri.ToString());
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal(jogadorId, body.RootElement.GetProperty("jogadorId").GetGuid());
            Assert.False(body.RootElement.TryGetProperty("chaveAtivacao", out _));
            Assert.Equal(chave, body.RootElement.GetProperty("chaveIdempotencia").GetGuid());
            Assert.Equal(3, body.RootElement.GetProperty("versaoEsperada").GetInt64());
            Assert.Equal(1, body.RootElement.GetProperty("lado").GetInt32());
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new ConfirmacaoAcao(chave, false, Estado(id))) };
        })) { BaseAddress = new Uri("http://localhost") };
        var cliente = new DominoApiClient(http);
        cliente.SelecionarJogador(jogadorId, ativacao, "https://cliente.example/webhook");
        var result = await cliente.JogarAsync("teste-local", id, new(6, 5), LadoJogavel.Direita, 3, chave);
        Assert.Equal(id, result.Estado.PartidaId);
    }

    [Theory]
    [InlineData(401, "ACESSO_INVALIDO")]
    [InlineData(403, "SEM_PERMISSAO")]
    [InlineData(404, "PARTIDA_INDISPONIVEL")]
    [InlineData(429, "LIMITE_EXCEDIDO")]
    public async Task RejeicaoNaoPodeSerConfundidaComSucesso(int status, string codigo)
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status))))
        { BaseAddress = new Uri("http://localhost") };
        var erro = await Assert.ThrowsAsync<ApiException>(() => new DominoApiClient(http).EstadoAsync("segredo", Guid.NewGuid()));
        Assert.Equal(codigo, erro.Codigo);
        Assert.DoesNotContain("segredo", erro.Message);
    }

    [Fact]
    public async Task ConflitoPreservaEstadoAutoritativoSemDeclararSucesso()
    {
        var id = Guid.NewGuid();
        using var http = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
        { Content = JsonContent.Create(new { codigo = "CONFLITO_DE_ESTADO", estadoAutoritativo = Estado(id) }) })))
        { BaseAddress = new Uri("http://localhost") };
        var erro = await Assert.ThrowsAsync<ApiException>(() => new DominoApiClient(http).AcaoAsync("segredo", id, "iniciar", 0, Guid.NewGuid()));
        Assert.Equal(id, erro.EstadoAutoritativo!.PartidaId);
        Assert.Equal(409, erro.Status);
    }

    [Fact]
    public async Task FalhaDeRedeNaoExpoeDetalhesDaExcecao()
    {
        using var http = new HttpClient(new Handler(_ => throw new HttpRequestException("detalhe-sensivel")))
        { BaseAddress = new Uri("http://localhost") };
        var erro = await Assert.ThrowsAsync<ApiException>(() => new DominoApiClient(http).SessaoAsync("segredo"));
        Assert.Equal(503, erro.Status);
        Assert.DoesNotContain("sensivel", erro.Message);
    }

    [Theory]
    [InlineData("http://api.exemplo.com")]
    [InlineData("https://usuario:senha@api.exemplo.com")]
    [InlineData("https://api.exemplo.com?token=segredo")]
    public void EnderecoNaoPermiteEnviarSegredoSemTlsOuNaUrl(string endereco) =>
        Assert.Throws<ArgumentException>(() => DominoApiClient.ValidarEndereco(endereco));

    internal static EstadoPartida Estado(Guid id) => new(id, 4, SituacaoPartida.EmAndamento, 1, null,
        [new(1, 2)], [new(6, 5)], [], null);

    internal sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);
    }
}
