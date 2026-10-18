using System.Globalization;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using DominoPontaDeQuina.Domain.Entities;
using Microsoft.Extensions.Options;

namespace DominoPontaDeQuina.Api.Webhooks;

public sealed class WebhookSender(HttpClient http, WebhookDestinoValidator destinos, IOptions<WebhookOptions> options)
{
    public async Task<(bool Sucesso, string Falha)> EnviarAsync(EventoWebhook evento, CancellationToken ct)
    {
        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(ct);
        prazo.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.Value.TimeoutSegundos, 1, 30)));
        try { return await EnviarComPrazoAsync(evento, prazo.Token); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return (false, "TIMEOUT"); }
    }

    private async Task<(bool Sucesso, string Falha)> EnviarComPrazoAsync(EventoWebhook evento, CancellationToken ct)
    {
        if (Encoding.UTF8.GetByteCount(evento.ConteudoJson) > 65536) return (false, "CONTEUDO_EXCEDIDO");
        if (string.IsNullOrWhiteSpace(options.Value.AssinaturaSegredo))
            return (false, "ASSINATURA_NAO_CONFIGURADA");
        if (!Uri.TryCreate(evento.Destino, UriKind.Absolute, out var destino) || !await destinos.EhPermitidoAsync(destino, ct))
            return (false, "DESTINO_NAO_PERMITIDO");

        var instante = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var assinatura = Assinar(instante, evento.ConteudoJson, options.Value.AssinaturaSegredo);
        using var request = new HttpRequestMessage(HttpMethod.Post, destino)
        {
            Content = new StringContent(evento.ConteudoJson, Encoding.UTF8, MediaTypeNames.Application.Json)
        };
        request.Headers.TryAddWithoutValidation("X-Domino-Evento", evento.Id.ToString("D"));
        request.Headers.TryAddWithoutValidation("X-Domino-Instante", instante);
        request.Headers.TryAddWithoutValidation("X-Domino-Assinatura", "sha256=" + assinatura);

        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            return response.IsSuccessStatusCode
                ? (true, string.Empty)
                : (false, $"HTTP_{(int)response.StatusCode}");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return (false, "TIMEOUT"); }
        catch (HttpRequestException) { return (false, "FALHA_HTTP"); }
    }

    internal static string Assinar(string instante, string corpo, string segredo)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(segredo));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(instante + "." + corpo))).ToLowerInvariant();
    }
}
