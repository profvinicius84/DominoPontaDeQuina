namespace DominoPontaDeQuina.Client;

/// <summary>Aplica estados recebidos por HTTP ou por clientes legados de callback sem executar regras de dominó.</summary>
public sealed class FluxoEstadoCliente
{
    private readonly object trava = new();
    private readonly List<EventoEstado> antecipados = [];
    private TaskCompletionSource sinal = NovoSinal();
    private bool emComando;
    private int geracao;
    public EstadoPartida? Estado { get; private set; }
    public event Action<EstadoPartida>? Atualizado;
    public TimeSpan EsperaCallback { get; init; } = TimeSpan.FromSeconds(3);

    public void Limpar()
    {
        lock (trava) { geracao++; Estado = null; antecipados.Clear(); emComando = false; Acordar(); }
    }

    public bool Receber(EventoEstado evento)
    {
        EstadoPartida? publicar = null;
        lock (trava)
        {
            if (evento.Estado is null || evento.EventoId == Guid.Empty || evento.ParticipacaoId == Guid.Empty
                || evento.PartidaId != evento.Estado.PartidaId || evento.ParticipacaoId != evento.Estado.ParticipacaoId
                || evento.VersaoEstado != evento.Estado.VersaoEstado) return false;
            if (Estado is not null && !MesmoDestino(evento.Estado, Estado)) return false;
            if (Estado is not null && evento.VersaoEstado <= Estado.VersaoEstado) return true;
            if (antecipados.Any(e => e.EventoId == evento.EventoId)) return true;
            if (emComando || Estado is null)
            {
                antecipados.Add(evento);
                if (antecipados.Count > 64) antecipados.RemoveAt(0);
            }
            else Estado = publicar = evento.Estado;
            Acordar();
        }
        if (publicar is not null) Atualizado?.Invoke(publicar);
        return true;
    }

    public void Recuperar(EstadoPartida estado)
    {
        lock (trava)
        {
            if (Estado is not null && (!MesmoDestino(estado, Estado) || estado.VersaoEstado < Estado.VersaoEstado)) return;
            Estado = estado;
        }
        Atualizado?.Invoke(estado);
    }

    public async Task<EstadoPartida> ExecutarAsync(Func<Task<EstadoPartida>> comando,
        Func<Task<EstadoPartida>> recuperar, CancellationToken ct = default)
    {
        int inicio;
        lock (trava)
        {
            if (emComando) throw new InvalidOperationException("Aguarde a ação anterior.");
            emComando = true; inicio = geracao;
        }
        try
        {
            var confirmado = await comando();
            if (EsperaCallback <= TimeSpan.Zero)
            {
                lock (trava)
                {
                    if (inicio != geracao) throw new OperationCanceledException();
                }
                Recuperar(confirmado);
                return confirmado;
            }
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limite.CancelAfter(EsperaCallback);
            while (true)
            {
                Task aguardar;
                EstadoPartida? recebido;
                lock (trava)
                {
                    if (inicio != geracao) throw new OperationCanceledException();
                    recebido = antecipados.Where(e => MesmoDestino(e.Estado, confirmado) && e.VersaoEstado >= confirmado.VersaoEstado)
                        .OrderByDescending(e => e.VersaoEstado).Select(e => e.Estado).FirstOrDefault();
                    aguardar = sinal.Task;
                    if (recebido is not null) { Estado = recebido; antecipados.Clear(); emComando = false; }
                }
                if (recebido is not null) { Atualizado?.Invoke(recebido); return recebido; }
                try { await aguardar.WaitAsync(limite.Token); }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested && limite.IsCancellationRequested)
                {
                    var atual = await recuperar();
                    lock (trava)
                    {
                        if (inicio != geracao) throw new OperationCanceledException();
                        var maisNovo = antecipados.Where(e => MesmoDestino(e.Estado, atual) && e.VersaoEstado > atual.VersaoEstado)
                            .OrderByDescending(e => e.VersaoEstado).FirstOrDefault();
                        Estado = atual = maisNovo?.Estado ?? atual;
                        antecipados.Clear(); emComando = false;
                    }
                    Atualizado?.Invoke(atual); return atual;
                }
            }
        }
        catch (ApiException erro)
        {
            if (erro.Status is 401 or 403 or 404) Limpar();
            else if (erro.EstadoAutoritativo is not null) Recuperar(erro.EstadoAutoritativo);
            // Rejeição sem mutação não espera evento.
            throw;
        }
        finally
        {
            EstadoPartida? publicar = null;
            lock (trava)
            {
                if (inicio == geracao)
                {
                    emComando = false;
                    if (Estado is not null)
                    {
                        var novo = antecipados.Where(e => MesmoDestino(e.Estado, Estado) && e.VersaoEstado > Estado.VersaoEstado)
                            .OrderByDescending(e => e.VersaoEstado).FirstOrDefault();
                        if (novo is not null) Estado = publicar = novo.Estado;
                        antecipados.Clear();
                    }
                }
            }
            if (publicar is not null) Atualizado?.Invoke(publicar);
        }
    }

    private static bool MesmoDestino(EstadoPartida a, EstadoPartida b) =>
        a.PartidaId == b.PartidaId && a.ParticipacaoId == b.ParticipacaoId;
    private static TaskCompletionSource NovoSinal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private void Acordar() { var anterior = sinal; sinal = NovoSinal(); anterior.TrySetResult(); }
}
