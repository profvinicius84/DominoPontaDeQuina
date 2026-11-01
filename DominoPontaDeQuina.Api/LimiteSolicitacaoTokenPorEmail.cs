using System.Security.Cryptography;
using System.Text;

namespace DominoPontaDeQuina.Api;

public sealed class LimiteSolicitacaoTokenPorEmail(TimeProvider tempo)
{
    private const int MaximoSolicitacoes = 5;
    private const int MaximoEmailsAtivos = 100_000;
    private static readonly TimeSpan Janela = TimeSpan.FromMinutes(10);
    private readonly object sincronizacao = new();
    private readonly Dictionary<string, Historico> historicos = new();
    private DateTimeOffset proximaLimpeza;

    public bool TentarRegistrar(string emailNormalizado)
    {
        var agora = tempo.GetUtcNow();
        var chave = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(emailNormalizado)));

        lock (sincronizacao)
        {
            if (agora >= proximaLimpeza)
            {
                foreach (var inativo in historicos.Where(item => agora - item.Value.UltimaSolicitacao >= Janela)
                             .Select(item => item.Key).ToArray())
                    historicos.Remove(inativo);
                proximaLimpeza = agora.AddMinutes(1);
            }

            if (!historicos.TryGetValue(chave, out var historico))
            {
                if (historicos.Count >= MaximoEmailsAtivos) return false;
                historicos[chave] = historico = new Historico();
            }

            while (historico.Solicitacoes.Count > 0 && agora - historico.Solicitacoes.Peek() >= Janela)
                historico.Solicitacoes.Dequeue();
            if (historico.Solicitacoes.Count >= MaximoSolicitacoes) return false;

            historico.Solicitacoes.Enqueue(agora);
            historico.UltimaSolicitacao = agora;
            return true;
        }
    }

    private sealed class Historico
    {
        public Queue<DateTimeOffset> Solicitacoes { get; } = new();
        public DateTimeOffset UltimaSolicitacao { get; set; }
    }
}
