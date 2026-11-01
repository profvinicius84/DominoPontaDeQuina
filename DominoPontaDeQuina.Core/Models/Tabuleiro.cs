using DominoPontaDeQuina.Core.Enums;

namespace DominoPontaDeQuina.Core.Models;

/// <summary>
/// Representa o tabuleiro no nivel da rodada dentro da hierarquia Partida -> Rodadas -> Jogadas.
/// Neste nivel ficam as pecas ja coladas e as informacoes necessarias para validar jogadas,
/// calcular pontuacao pelas pontas externas e verificar situacoes de travamento.
/// </summary>
public class Tabuleiro
{
    /// <summary>
    /// Obtem as pecas posicionadas no tabuleiro na ordem em que foram coladas.
    /// </summary>
    public List<Peca> Pecas { get; } = [];

    /// <summary>
    /// Indica se o tabuleiro ainda nao possui pecas coladas.
    /// </summary>
    public bool EstaVazio => Pecas.Count == 0;

    /// <summary>
    /// Obtem a ponta esquerda atualmente exposta no tabuleiro.
    /// Quando o tabuleiro estiver vazio, nao existe ponta externa disponivel.
    /// </summary>
    public int? PontaEsquerda => EstaVazio ? null : Pecas[0].ValorA;

    /// <summary>
    /// Obtem a ponta direita atualmente exposta no tabuleiro.
    /// Quando o tabuleiro estiver vazio, nao existe ponta externa disponivel.
    /// </summary>
    public int? PontaDireita => EstaVazio ? null : Pecas[^1].ValorB;

    /// <summary>
    /// Determina se uma peca pode ser colada no lado informado.
    /// A regra esperada e validar se a peca possui valor compativel com a ponta externa do lado escolhido.
    /// </summary>
    /// <param name="peca">A peca a ser verificada.</param>
    /// <param name="lado">O lado do tabuleiro.</param>
    /// <returns><see langword="true"/> quando a peca puder ser colada; caso contrario, <see langword="false"/>.</returns>
    public bool PodeColar(Peca peca, LadoTabuleiro lado)
    {
        return EstaVazio || peca.PossuiValor(lado == LadoTabuleiro.Esquerda
            ? PontaEsquerda!.Value
            : PontaDireita!.Value);
    }

    /// <summary>
    /// Cola uma peca no lado informado do tabuleiro.
    /// A regra esperada e posicionar a peca no lado correto, invertendo seus valores quando necessario.
    /// </summary>
    /// <param name="peca">A peca a ser colada.</param>
    /// <param name="lado">O lado do tabuleiro.</param>
    public void Colar(Peca peca, LadoTabuleiro lado)
    {
        if (!PodeColar(peca, lado))
            throw new InvalidOperationException("A peça não é compatível com a ponta escolhida.");

        if (EstaVazio)
        {
            Pecas.Add(peca);
            return;
        }

        if (lado == LadoTabuleiro.Esquerda)
        {
            var orientada = peca.ValorB == PontaEsquerda ? peca : peca.Inverter();
            Pecas.Insert(0, orientada);
            return;
        }

        var orientadaDireita = peca.ValorA == PontaDireita ? peca : peca.Inverter();
        Pecas.Add(orientadaDireita);
    }

    /// <summary>
    /// Soma os valores das pontas externas atualmente expostas.
    /// Essa soma e a base para regras de pontuacao em que a rodada concede pontos quando o resultado for multiplo de 5.
    /// Uma dupla na extremidade contribui com suas duas faces. Se for a única peça,
    /// ambas as faces são contadas uma única vez.
    /// </summary>
    /// <returns>A soma das pontas externas, ou 0 quando o tabuleiro estiver vazio.</returns>
    public int SomarPontasExternas()
    {
        if (EstaVazio) return 0;
        if (Pecas.Count == 1) return Pecas[0].ValorA + Pecas[0].ValorB;

        var esquerda = Pecas[0];
        var direita = Pecas[^1];
        return (esquerda.EhDupla ? esquerda.ValorA + esquerda.ValorB : esquerda.ValorA)
            + (direita.EhDupla ? direita.ValorA + direita.ValorB : direita.ValorB);
    }

    /// <summary>
    /// Determina se o tabuleiro esta travado.
    /// O travamento e esperado quando nenhuma mao de jogador possuir peca compativel com as pontas externas atuais.
    /// </summary>
    /// <param name="maosJogadores">As maos dos jogadores da rodada.</param>
    /// <returns><see langword="true"/> quando o tabuleiro estiver travado; caso contrario, <see langword="false"/>.</returns>
    public bool EstaTravado(IEnumerable<MaoJogador> maosJogadores)
    {
        ArgumentNullException.ThrowIfNull(maosJogadores);
        return !EstaVazio && !maosJogadores.Any(mao =>
            mao.Pecas.Any(peca => PodeColar(peca, LadoTabuleiro.Esquerda) || PodeColar(peca, LadoTabuleiro.Direita)));
    }

    /// <summary>
    /// Limpa o tabuleiro para preparar uma nova rodada.
    /// </summary>
    public void Limpar() =>
        Pecas.Clear();
}
