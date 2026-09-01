namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>Representa uma partida persistida e seu histórico de atividade.</summary>
public class Partida
{
    /// <summary>Obtém ou define o identificador da partida.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Obtém ou define a pontuação necessária para vencer.</summary>
    public int PontuacaoAlvo { get; set; }
    /// <summary>Obtém ou define o status atual da partida.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Obtém ou define a data de criação da partida em UTC.</summary>
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    /// <summary>Obtém as participações registradas na partida.</summary>
    public ICollection<ParticipacaoPartida> Participacoes { get; set; } = [];
    /// <summary>Obtém os lances registrados na partida.</summary>
    public ICollection<Lance> Lances { get; set; } = [];
}
