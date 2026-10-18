using DominoPontaDeQuina.Blazor;
using DominoPontaDeQuina.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient<DominoApiClient>(client =>
{
    client.BaseAddress = DominoApiClient.ValidarEndereco(builder.Configuration["DominoApi:BaseUrl"] ?? "http://localhost:5180");
    client.Timeout = TimeSpan.FromSeconds(15);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
