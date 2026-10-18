// Adaptador exclusivamente local: TLS na entrada e destinos fixos em loopback.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 65536);
builder.Services.AddHttpClient("receptor", c => c.Timeout = TimeSpan.FromSeconds(3))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false, UseCookies = false });
var app = builder.Build();
app.MapPost("/clientes/{cliente}", async (string cliente, HttpContext context, IHttpClientFactory clients) =>
{
    if (context.Connection.RemoteIpAddress is not { } ip || !System.Net.IPAddress.IsLoopback(ip)) return Results.NotFound();
    var porta = cliente switch { "a" => 5191, "b" => 5192, _ => 0 };
    if (porta == 0) return Results.NotFound();
    using var request = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{porta}/webhook/")
    { Content = new StreamContent(context.Request.Body) };
    request.Content.Headers.ContentType = new("application/json");
    foreach (var header in new[] { "X-Domino-Evento", "X-Domino-Instante", "X-Domino-Assinatura" })
        request.Headers.TryAddWithoutValidation(header, context.Request.Headers[header].ToString());
    try
    {
        using var resposta = await clients.CreateClient("receptor").SendAsync(request, context.RequestAborted);
        return Results.StatusCode((int)resposta.StatusCode);
    }
    catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
    { return Results.StatusCode(503); }
});
app.Run();
