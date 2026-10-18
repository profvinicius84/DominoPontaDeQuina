using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DominoPontaDeQuina.Api.Webhooks;
using DominoPontaDeQuina.Client;

namespace DominoPontaDeQuina.Tests;

public sealed class FluxoClienteWebhookTests
{
    private static EstadoPartida Estado(long versao = 1) => ClientTests.Estado(Guid.NewGuid()) with
    { VersaoEstado = versao, ParticipacaoId = Guid.NewGuid() };
    private static EventoEstado Evento(EstadoPartida e) => new(Guid.NewGuid(), e.PartidaId, e.ParticipacaoId,
        e.VersaoEstado, "teste", DateTime.UtcNow, e);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CallbackAntesOuDepoisDoHttp_AplicaSomenteAposConfirmacao(bool antes)
    {
        var fluxo = new FluxoEstadoCliente(); var e = Estado();
        var http = new TaskCompletionSource<EstadoPartida>();
        var operacao = fluxo.ExecutarAsync(() => http.Task, () => throw new Exception("Não deve consultar"));
        if (antes) { Assert.True(fluxo.Receber(Evento(e))); Assert.Null(fluxo.Estado); }
        http.SetResult(e);
        if (!antes) Assert.True(fluxo.Receber(Evento(e)));
        Assert.Equal(e, await operacao);
    }

    [Fact]
    public void DuplicadoAntigoEOutroDestinatario_NaoRegredemEstado()
    {
        var fluxo = new FluxoEstadoCliente(); var e = Estado(); fluxo.Recuperar(e);
        var novo = Evento(e with { VersaoEstado = 5 });
        fluxo.Receber(novo); fluxo.Receber(novo); fluxo.Receber(Evento(e with { VersaoEstado = 3 }));
        Assert.False(fluxo.Receber(Evento(Estado(20))));
        Assert.Equal(5, fluxo.Estado!.VersaoEstado);
    }

    [Fact]
    public async Task AusenciaDeEvento_RecuperaERejeicaoNaoEspera()
    {
        var fluxo = new FluxoEstadoCliente { EsperaCallback = TimeSpan.FromMilliseconds(15) }; var e = Estado();
        var consultas = 0;
        Assert.Equal(e, await fluxo.ExecutarAsync(() => Task.FromResult(e), () => { consultas++; return Task.FromResult(e); }));
        Assert.Equal(1, consultas);
        await Assert.ThrowsAsync<ApiException>(() => fluxo.ExecutarAsync(() => throw new ApiException(422, "ESTADO_INVALIDO"),
            () => throw new Exception("Rejeição não consulta nem espera")));
        Assert.Equal(e, fluxo.Estado);
        await Assert.ThrowsAsync<ApiException>(() => fluxo.ExecutarAsync(() => throw new ApiException(401, "NAO_AUTENTICADO"),
            () => Task.FromResult(e)));
        Assert.Null(fluxo.Estado);
    }

    [Fact]
    public async Task EventoDuranteConsultaDeRecuperacao_NaoSePerde()
    {
        var fluxo = new FluxoEstadoCliente { EsperaCallback = TimeSpan.FromMilliseconds(10) }; var e = Estado();
        var final = await fluxo.ExecutarAsync(() => Task.FromResult(e), () =>
        { fluxo.Receber(Evento(e with { VersaoEstado = 7 })); return Task.FromResult(e); });
        Assert.Equal(7, final.VersaoEstado);
    }

    [Fact]
    public async Task SairDuranteHttp_NaoRestauraDadosPrivados()
    {
        var fluxo = new FluxoEstadoCliente(); var http = new TaskCompletionSource<EstadoPartida>();
        var operacao = fluxo.ExecutarAsync(() => http.Task, () => Task.FromResult(Estado()));
        fluxo.Limpar(); http.SetResult(Estado());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operacao);
        Assert.Null(fluxo.Estado);
    }

    [Theory]
    [InlineData(0, false, true)]
    [InlineData(-301, false, false)]
    [InlineData(301, false, false)]
    [InlineData(0, true, false)]
    public void AssinaturaExigeCorpoOriginalEInstanteRecente(int deslocamento, bool adulterar, bool esperado)
    {
        var agora = DateTimeOffset.UtcNow; var segredo = new string('s', 32);
        var json = JsonSerializer.Serialize(Evento(Estado()));
        var instante = agora.AddSeconds(deslocamento).ToUnixTimeSeconds().ToString();
        var assinatura = "sha256=" + WebhookSender.Assinar(instante, json, segredo);
        if (adulterar) json += " ";
        Assert.Equal(esperado, ReceptorWebhook.Verificar(Encoding.UTF8.GetBytes(json), instante, assinatura, segredo, agora) is not null);
    }

    [Fact]
    public async Task ReceptorLocalRecebePostRealAssinadoERejeitaSemAssinatura()
    {
        var portaLivre = new TcpListener(IPAddress.Loopback, 0); portaLivre.Start();
        var porta = ((IPEndPoint)portaLivre.LocalEndpoint).Port; portaLivre.Stop();
        var recebido = new TaskCompletionSource<EventoEstado>(TaskCreationOptions.RunContinuationsAsynchronously);
        var segredo = new string('s', 32);
        await using var receptor = new ReceptorWebhook(porta, segredo, e => { recebido.TrySetResult(e); return true; });
        receptor.Iniciar(); using var http = new HttpClient();
        var json = JsonSerializer.Serialize(Evento(Estado())); var instante = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{porta}/webhook/") { Content = new StringContent(json) };
        request.Headers.Add("X-Domino-Instante", instante);
        request.Headers.Add("X-Domino-Assinatura", "sha256=" + WebhookSender.Assinar(instante, json, segredo));
        Assert.Equal(HttpStatusCode.NoContent, (await http.SendAsync(request)).StatusCode);
        Assert.NotNull(await recebido.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.PostAsync($"http://localhost:{porta}/webhook/", new StringContent(json))).StatusCode);
    }
}
