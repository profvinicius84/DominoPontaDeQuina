namespace DominoPontaDeQuina.Domain.Entities;

public class ParticipacaoJogo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JogoId { get; set; }
    public Jogo Jogo { get; set; } = null!;
    public Guid JogadorId { get; set; }
    public Jogador Jogador { get; set; } = null!;
    public int Posicao { get; set; }
    public int Pontuacao { get; set; }
    public bool Vencedor { get; set; }
}
