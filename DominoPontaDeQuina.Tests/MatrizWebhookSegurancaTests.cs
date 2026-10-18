using DominoPontaDeQuina.Api.Webhooks;
using DominoPontaDeQuina.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace DominoPontaDeQuina.Tests;

public sealed class MatrizWebhookSegurancaTests
{
    [Theory]
    [InlineData("https://127.0.0.1/a")]
    [InlineData("https://[::ffff:127.0.0.1]/a")]
    [InlineData("https://[::1]/a")]
    [InlineData("https://10.0.0.1/a")]
    [InlineData("https://172.16.0.1/a")]
    [InlineData("https://192.168.0.1/a")]
    [InlineData("https://169.254.169.254/a")]
    [InlineData("https://100.64.0.1/a")]
    [InlineData("https://[fd00::1]/a")]
    [InlineData("https://[fe80::1]/a")]
    [InlineData("https://[::]/a")]
    [InlineData("https://[ff02::1]/a")]
    [InlineData("http://8.8.8.8/a")]
    [InlineData("https://user:pass@8.8.8.8/a")]
    public async Task DestinosInternosOuNaoHttpsSaoBloqueados(string url)
    {
        var validator = new WebhookDestinoValidator(new Ambiente(), Options.Create(new WebhookOptions()));
        Assert.False(await validator.EhPermitidoAsync(new Uri(url), default));
    }

    [Fact]
    public async Task TimeoutDeEntregaEhLimitadoENaoVazaMensagem()
    {
        var options = Options.Create(new WebhookOptions { TimeoutSegundos = 1, AssinaturaSegredo = new string('s', 32) });
        using var http = new HttpClient(new EsperaHandler());
        var sender = new WebhookSender(http, new WebhookDestinoValidator(new Ambiente(), options), options);
        var resultado = await sender.EnviarAsync(new EventoWebhook { Destino = "https://8.8.8.8/webhook", ConteudoJson = "{}" }, default);
        Assert.False(resultado.Sucesso); Assert.Equal("TIMEOUT", resultado.Falha);
    }

    private sealed class EsperaHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        { await Task.Delay(Timeout.InfiniteTimeSpan, ct); return new(); }
    }
    private sealed class Ambiente : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Teste";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
