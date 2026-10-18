using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DominoPontaDeQuina.Client;

/// <summary>Receptor local para HTTPS terminado por proxy; nunca escuta em interface pública.</summary>
public sealed class ReceptorWebhook : IAsyncDisposable
{
    private readonly HttpListener listener = new();
    private readonly CancellationTokenSource parada = new();
    private readonly string segredo;
    private readonly Func<EventoEstado, bool> receber;
    private Task? execucao;
    public ReceptorWebhook(int porta, string segredo, Func<EventoEstado, bool> receber)
    {
        if (porta is < 1024 or > 65535 || segredo.Length < 32) throw new ArgumentException("Porta ou segredo do receptor inválido.");
        this.segredo = segredo; this.receber = receber;
        listener.Prefixes.Add($"http://localhost:{porta}/webhook/");
    }
    public void Iniciar() { listener.Start(); execucao = ExecutarAsync(); }
    public static EventoEstado? Verificar(byte[] corpo, string? instante, string? assinatura, string segredo, DateTimeOffset agora)
    {
        if (corpo.Length > 65536 || !long.TryParse(instante, NumberStyles.None, CultureInfo.InvariantCulture, out var segundos)
            || segundos < agora.ToUnixTimeSeconds() - 300 || segundos > agora.ToUnixTimeSeconds() + 300
            || assinatura is null || !assinatura.StartsWith("sha256=", StringComparison.Ordinal)) return null;
        byte[] fornecida;
        try { fornecida = Convert.FromHexString(assinatura[7..]); } catch (FormatException) { return null; }
        var prefixo = Encoding.UTF8.GetBytes(instante + ".");
        var dados = new byte[prefixo.Length + corpo.Length];
        prefixo.CopyTo(dados, 0); corpo.CopyTo(dados, prefixo.Length);
        var esperada = HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), dados);
        if (!CryptographicOperations.FixedTimeEquals(esperada, fornecida)) return null;
        try { return JsonSerializer.Deserialize<EventoEstado>(corpo, Json); } catch (JsonException) { return null; }
    }
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };
    private async Task ExecutarAsync()
    {
        while (!parada.IsCancellationRequested)
        {
            HttpListenerContext contexto;
            try { contexto = await listener.GetContextAsync().WaitAsync(parada.Token); }
            catch (Exception ex) when (parada.IsCancellationRequested && ex is OperationCanceledException or HttpListenerException or ObjectDisposedException) { break; }
            try
            {
                contexto.Response.StatusCode = 400;
                using var prazo = CancellationTokenSource.CreateLinkedTokenSource(parada.Token);
                prazo.CancelAfter(TimeSpan.FromSeconds(3));
                if (contexto.Request.HttpMethod != "POST") { contexto.Response.StatusCode = 405; continue; }
                using var buffer = new MemoryStream();
                var bloco = new byte[4096]; int lidos;
                while ((lidos = await contexto.Request.InputStream.ReadAsync(bloco, prazo.Token)) > 0)
                {
                    if (buffer.Length + lidos > 65536) { contexto.Response.StatusCode = 413; break; }
                    buffer.Write(bloco, 0, lidos);
                }
                if (contexto.Response.StatusCode == 413) continue;
                var evento = Verificar(buffer.ToArray(), contexto.Request.Headers["X-Domino-Instante"],
                    contexto.Request.Headers["X-Domino-Assinatura"], segredo, DateTimeOffset.UtcNow);
                contexto.Response.StatusCode = evento is null ? 401 : receber(evento) ? 204 : 400;
            }
            catch (Exception ex) when (ex is OperationCanceledException or IOException or HttpListenerException)
            { contexto.Response.StatusCode = 503; }
            finally { contexto.Response.Close(); }
        }
    }
    public async ValueTask DisposeAsync()
    {
        await parada.CancelAsync(); listener.Close();
        if (execucao is not null) await execucao;
        parada.Dispose();
    }
}
