namespace DominoPontaDeQuina.Core.Models;

/// <summary>
/// Representa a unidade basica do dominio no jogo de domino.
/// A peca e usada ao longo da hierarquia Partida -> Rodadas -> Jogadas como elemento que pode ser distribuido,
/// mantido na mao de um jogador e colado no tabuleiro durante a rodada.
/// </summary>
/// <param name="valorA">O primeiro valor da peca.</param>
/// <param name="valorB">O segundo valor da peca.</param>
public readonly struct Peca(int valorA, int valorB) : IEquatable<Peca>
{
    /// <summary>
    /// Obtem o valor do primeiro lado da peca.
    /// </summary>
    public int ValorA { get; } = valorA;

    /// <summary>
    /// Obtem o valor do segundo lado da peca.
    /// </summary>
    public int ValorB { get; } = valorB;

    /// <summary>
    /// Obtem a soma dos valores da peca.
    /// Essa informacao e util em regras que avaliam a menor soma das pecas restantes na mao.
    /// </summary>
    public int SomaValores => ValorA + ValorB;

    /// <summary>
    /// Indica se a peca e a sena, isto e, a peca [6|6].
    /// Essa verificacao e relevante para regras em que a primeira rodada comeca com quem possui a sena.
    /// </summary>
    public bool EhSena => ValorA == 6 && ValorB == 6;

    /// <summary>Indica se os dois lados da peça possuem o mesmo valor.</summary>
    public bool EhDupla => ValorA == ValorB;

    /// <summary>Obtém a maior face da peça para ordenações de abertura.</summary>
    public int MaiorFace => Math.Max(ValorA, ValorB);

    /// <summary>Obtém a menor face da peça para ordenações de abertura.</summary>
    public int MenorFace => Math.Min(ValorA, ValorB);

    /// <summary>
    /// Determina se a peca possui o valor informado em qualquer um de seus lados.
    /// Essa verificacao e base para regras de compatibilidade com as pontas do tabuleiro.
    /// </summary>
    /// <param name="valor">O valor a ser verificado.</param>
    /// <returns><see langword="true"/> quando a peca possuir o valor; caso contrario, <see langword="false"/>.</returns>
    public bool PossuiValor(int valor) =>
        ValorA == valor || ValorB == valor;

    /// <summary>
    /// Retorna uma nova peca com os valores invertidos.
    /// Essa operacao e util quando a jogada exige reorganizar os lados para encaixe no tabuleiro.
    /// </summary>
    /// <returns>Uma nova peca invertida.</returns>
    public Peca Inverter() =>
        new(ValorB, ValorA);

    /// <summary>Compara a pedra sem considerar a orientação de seus lados.</summary>
    public bool Equals(Peca other) =>
        MaiorFace == other.MaiorFace && MenorFace == other.MenorFace;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Peca other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(MaiorFace, MenorFace);

    /// <inheritdoc />
    public override string ToString() =>
        $"[{ValorA}|{ValorB}]";
}
