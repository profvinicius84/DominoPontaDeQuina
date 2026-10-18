namespace DominoPontaDeQuina.Core.Models;

/// <summary>
/// Representa o jogador na hierarquia Partida -> Rodadas -> Jogadas.
/// Neste nivel ficam apenas a identidade e os dados persistentes do participante.
/// As pecas usadas durante a partida ficam separadas em <see cref="MaoJogador"/>.
/// </summary>
/// <param name="nome">O nome do jogador.</param>
public class Jogador(string nome)
{
    /// <summary>Reconstrói a identidade e a pontuação de um jogador persistido.</summary>
    public Jogador(Guid id, string nome, int pontuacao = 0) : this(nome)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("O identificador do jogador é obrigatório.", nameof(id));
        if (pontuacao < 0)
            throw new ArgumentOutOfRangeException(nameof(pontuacao));

        Id = id;
        Pontuacao = pontuacao;
    }
    /// <summary>
    /// Obtem o identificador unico do jogador.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Obtem o nome exibido do jogador na partida.
    /// </summary>
    public string Nome { get; } = nome;

    /// <summary>Obtém a pontuação acumulada pelo jogador na partida.</summary>
    public int Pontuacao { get; private set; }

    /// <summary>Soma pontos obtidos em uma rodada.</summary>
    public void SomarPontos(int pontos)
    {
        if (pontos < 0)
            throw new ArgumentOutOfRangeException(nameof(pontos));

        Pontuacao += pontos;
    }
}
