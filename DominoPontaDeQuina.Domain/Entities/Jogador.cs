namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>Representa um jogador que pode participar de várias <see cref="Partida"/>.</summary>
public class Jogador
{
    /// <summary>Obtém ou define o identificador do jogador.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Obtém ou define o nome exibido do jogador.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Obtém a conta de autenticação associada, quando houver.</summary>
    public Usuario? Usuario { get; set; }
    /// <summary>Obtém as participações do jogador nas partidas.</summary>
    public ICollection<ParticipacaoPartida> Participacoes { get; set; } = [];
    /// <summary>Obtém os lances realizados pelo jogador.</summary>
    public ICollection<Lance> Lances { get; set; } = [];
    /// <summary>Obtém o ranking individual do jogador, quando cadastrado.</summary>
    public Ranking? Ranking { get; set; }
}
