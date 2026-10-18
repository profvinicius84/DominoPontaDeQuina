using DominoPontaDeQuina.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace DominoPontaDeQuina.Api.Webhooks;

public sealed class WebhookOutboxWorker(IServiceScopeFactory scopes, IOptions<WebhookOptions> options, ILogger<WebhookOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processou = await ProcessarUmAsync(stoppingToken);
                if (!processou)
                {
                    await LimparExpiradosAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.Value.IntervaloVarreduraSegundos)), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception)
            {
                logger.LogError("Falha ao processar a caixa de saída de webhooks; consulte a disponibilidade do banco.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    internal async Task<bool> ProcessarUmAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<IEventoWebhookRepository>();
        var evento = await repositorio.ReservarProximoAsync(DateTime.UtcNow, TimeSpan.FromSeconds(Math.Max(10, options.Value.TimeoutSegundos * 2)), ct);
        if (evento is null) return false;

        var sender = scope.ServiceProvider.GetRequiredService<WebhookSender>();
        var (sucesso, falha) = await sender.EnviarAsync(evento, ct);
        if (sucesso)
        {
            await repositorio.MarcarEntregueAsync(evento.Id, DateTime.UtcNow, ct);
            return true;
        }

        var abandonar = evento.Tentativas >= Math.Max(1, options.Value.MaximoTentativas) || falha == "DESTINO_NAO_PERMITIDO";
        var atrasoSegundos = Math.Min(900, Math.Pow(2, Math.Min(evento.Tentativas, 9)));
        await repositorio.MarcarFalhaAsync(evento.Id, DateTime.UtcNow.AddSeconds(atrasoSegundos), falha, abandonar, ct);
        logger.LogWarning("Entrega de webhook {EventoId} falhou com {Falha}; tentativa {Tentativa}.", evento.Id, falha, evento.Tentativas);
        return true;
    }

    private async Task LimparExpiradosAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<IEventoWebhookRepository>();
        var agora = DateTime.UtcNow;
        await repositorio.RemoverExpiradosAsync(
            agora.AddDays(-Math.Max(1, options.Value.RetencaoEntreguesDias)),
            agora.AddDays(-Math.Max(1, options.Value.RetencaoFalhasDias)), ct);
    }
}
