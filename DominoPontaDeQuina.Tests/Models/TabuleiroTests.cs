using DominoPontaDeQuina.Core.Models;
using DominoPontaDeQuina.Core.Enums;

namespace DominoPontaDeQuina.Tests.Models;

/// <summary>
/// Testes da classe Tabuleiro - gerencia o estado das peças coladas durante a rodada.
/// </summary>
public class TabuleiroTests
{
    #region Pontas do Tabuleiro

    /// <summary>
    /// <b>Objetivo:</b> Verificar se o tabuleiro retorna corretamente as pontas extremas (esquerda e direita) após múltiplas peças serem coladas.
    /// <br/><b>Critério:</b> A ponta esquerda deve ser o ValorA da primeira peça, e a ponta direita o ValorB da última peça.
    /// <br/><b>O que o aluno deve garantir:</b> A ordem das peças e o cálculo das pontas devem estar corretos após várias colagens.
    /// </summary>
    [Trait("Categoria", "Basico")]
    [Fact(DisplayName = "Deve retornar corretamente as pontas extremas após múltiplas peças coladas (Critério: ponta esquerda = ValorA da primeira peça, ponta direita = ValorB da última peça).")]
    public void Tabuleiro_Pontas_DeveRetornarPontasExtremosComMultiplasPecas()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();
        tabuleiro.Pecas.Add(new Peca(3, 5));
        tabuleiro.Pecas.Add(new Peca(5, 2));
        tabuleiro.Pecas.Add(new Peca(2, 4));

        // Act
        var pontaEsquerda = tabuleiro.PontaEsquerda;
        var pontaDireita = tabuleiro.PontaDireita;

        // Assert
        Assert.Equal(3, pontaEsquerda);
        Assert.Equal(4, pontaDireita);
    }

    #endregion

    #region SomarPontasExternas

    /// <summary>
    /// <b>Objetivo:</b> Garantir que a soma das pontas externas está correta quando há apenas uma peça no tabuleiro.
    /// <br/><b>Critério:</b> Soma deve ser igual a ValorA + ValorB da única peça.
    /// <br/><b>O que o aluno deve garantir:</b> O método de soma deve funcionar corretamente para o caso mais simples.
    /// </summary>
    [Trait("Categoria", "Basico")]
    [Fact(DisplayName = "Deve retornar a soma correta das pontas externas com uma peça (Critério: soma = ValorA + ValorB).")]
    public void Tabuleiro_SomarPontasExternas_DeveRetornarSomaCorretaComUmaPeca()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();
        tabuleiro.Pecas.Add(new Peca(3, 5));

        // Act
        var soma = tabuleiro.SomarPontasExternas();

        // Assert
        Assert.Equal(8, soma); // 3 + 5
    }

    /// <summary>
    /// <b>Objetivo:</b> Verificar se a soma das pontas considera apenas os extremos quando há múltiplas peças.
    /// <br/><b>Critério:</b> Soma deve ser igual a ValorA da primeira peça + ValorB da última peça.
    /// <br/><b>O que o aluno deve garantir:</b> O método não deve somar valores intermediários.
    /// </summary>
    [Trait("Categoria", "Basico")]
    [Fact(DisplayName = "Deve retornar a soma apenas das pontas extremas com múltiplas peças (Critério: soma = ValorA da primeira + ValorB da última).")]
    public void Tabuleiro_SomarPontasExternas_DeveRetornarSomaApenasDosPontosExtremosComMultiplasPecas()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();
        tabuleiro.Pecas.Add(new Peca(3, 5));
        tabuleiro.Pecas.Add(new Peca(5, 2));
        tabuleiro.Pecas.Add(new Peca(2, 4));

        // Act
        var soma = tabuleiro.SomarPontasExternas();

        // Assert
        // Apenas as pontas extremas: 3 (esquerda) + 4 (direita) = 7
        Assert.Equal(7, soma);
    }

    #endregion

    #region Limpar

    /// <summary>
    /// <b>Objetivo:</b> Garantir que ao limpar o tabuleiro, a soma das pontas é zerada.
    /// <br/><b>Critério:</b> Soma antes da limpeza deve ser maior que zero, e após a limpeza deve ser zero.
    /// <br/><b>O que o aluno deve garantir:</b> O método Limpar deve remover todas as peças e resetar o estado.
    /// </summary>
    [Trait("Categoria", "Basico")]
    [Fact(DisplayName = "Deve zerar a soma das pontas após limpar o tabuleiro (Critério: soma antes > 0, depois = 0).")]
    public void Tabuleiro_Limpar_DeveZerarSomaDosPontos()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();
        tabuleiro.Pecas.Add(new Peca(6, 6));
        var somaAntes = tabuleiro.SomarPontasExternas();

        // Act
        tabuleiro.Limpar();
        var somaDepois = tabuleiro.SomarPontasExternas();

        // Assert
        Assert.Equal(12, somaAntes);
        Assert.Equal(0, somaDepois);
    }

    #endregion

    #region Cenários Relevantes

    /// <summary>
    /// <b>Objetivo:</b> Simular uma sequência de jogadas e verificar se a soma das pontas é atualizada corretamente a cada jogada.
    /// <br/><b>Critério:</b> Soma correta após cada jogada, refletindo o estado do tabuleiro.
    /// <br/><b>O que o aluno deve garantir:</b> O método de soma deve ser dinâmico e responder a mudanças no tabuleiro.
    /// </summary>
    [Trait("Categoria", "Basico")]
    [Fact(DisplayName = "Simula sequência de jogadas e verifica soma das pontas após cada jogada.")]
    public void Tabuleiro_AoAdicionarMultiplasPecas_DeveManterSomaCorreta()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();

        // Act - Simula uma sequência de jogadas
        var peca1 = new Peca(6, 6); // Primeira peça (Sena)
        tabuleiro.Pecas.Add(peca1);
        var soma1 = tabuleiro.SomarPontasExternas();

        var peca2 = new Peca(6, 4);
        tabuleiro.Pecas.Add(peca2);
        var soma2 = tabuleiro.SomarPontasExternas();

        var peca3 = new Peca(4, 2);
        tabuleiro.Pecas.Add(peca3);
        var soma3 = tabuleiro.SomarPontasExternas();

        // Assert
        Assert.Equal(12, soma1); // 6 + 6
        Assert.Equal(16, soma2); // 6 + 6 + 4
        Assert.Equal(14, soma3); // 6 + 6 + 2
        Assert.Equal(3, tabuleiro.Pecas.Count);
    }

    /// <summary>
    /// <b>Objetivo:</b> Verificar se as pontas do tabuleiro mudam corretamente após cada nova peça colada.
    /// <br/><b>Critério:</b> Ponta esquerda e direita devem ser atualizadas conforme esperado após cada jogada.
    /// <br/><b>O que o aluno deve garantir:</b> O cálculo das pontas deve ser dinâmico e refletir o estado real do tabuleiro.
    /// </summary>
    [Trait("Categoria", "Basico")]
    [Fact(DisplayName = "Verifica se as pontas mudam corretamente após cada nova peça.")]
    public void Tabuleiro_Pontas_DeveMudarComNovasPecas()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();

        // Act & Assert - Monitorar mudanças de pontas
        tabuleiro.Pecas.Add(new Peca(2, 5));
        Assert.Equal(2, tabuleiro.PontaEsquerda);
        Assert.Equal(5, tabuleiro.PontaDireita);

        tabuleiro.Pecas.Add(new Peca(5, 3));
        Assert.Equal(2, tabuleiro.PontaEsquerda);
        Assert.Equal(3, tabuleiro.PontaDireita);

        tabuleiro.Pecas.Add(new Peca(3, 6));
        Assert.Equal(2, tabuleiro.PontaEsquerda);
        Assert.Equal(6, tabuleiro.PontaDireita);
    }

    #endregion

    [Fact(DisplayName = "Dupla exposta à direita soma ambas as faces sem alterar o encaixe")]
    public void Tabuleiro_DuplaNaDireita_SomaQuinze()
    {
        var tabuleiro = new Tabuleiro();
        tabuleiro.Colar(new Peca(3, 6), LadoTabuleiro.Direita);
        tabuleiro.Colar(new Peca(6, 6), LadoTabuleiro.Direita);

        Assert.Equal(3, tabuleiro.PontaEsquerda);
        Assert.Equal(6, tabuleiro.PontaDireita);
        Assert.Equal(15, tabuleiro.SomarPontasExternas());
    }

    [Fact(DisplayName = "Dupla exposta à esquerda soma ambas as faces")]
    public void Tabuleiro_DuplaNaEsquerda_SomaQuinze()
    {
        var tabuleiro = new Tabuleiro();
        tabuleiro.Colar(new Peca(6, 3), LadoTabuleiro.Direita);
        tabuleiro.Colar(new Peca(6, 6), LadoTabuleiro.Esquerda);

        Assert.Equal(15, tabuleiro.SomarPontasExternas());
    }

    [Fact(DisplayName = "Exemplo documentado mantém ordem e pontas livres")]
    public void Tabuleiro_ExemploDocumentado_ConfereOrientacao()
    {
        var tabuleiro = new Tabuleiro();
        tabuleiro.Colar(new Peca(3, 5), LadoTabuleiro.Direita);
        tabuleiro.Colar(new Peca(5, 6), LadoTabuleiro.Direita);
        tabuleiro.Colar(new Peca(6, 6), LadoTabuleiro.Direita);

        Assert.Equal(new[] { new Peca(3, 5), new Peca(5, 6), new Peca(6, 6) }, tabuleiro.Pecas);
        Assert.Equal(3, tabuleiro.PontaEsquerda);
        Assert.Equal(6, tabuleiro.PontaDireita);
        Assert.Equal(15, tabuleiro.SomarPontasExternas());
    }

    #region Gaps de Implementação

    /// <summary>
    /// <b>Objetivo:</b> Garantir que a primeira peça pode ser colada em qualquer lado do tabuleiro.
    /// <br/><b>Critério:</b> PodeColar deve retornar true para ambos os lados quando o tabuleiro está vazio.
    /// <br/><b>O que o aluno deve garantir:</b> A lógica de validação deve considerar o estado inicial do tabuleiro.
    /// </summary>
    [Trait("Categoria", "Gap")]
    [Fact(DisplayName = "Deve permitir colar a primeira peça em qualquer lado.")]
    public void Tabuleiro_PodeColar_DevePermitirPrimeiraPecaEmQualquerLado()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();
        var peca = new Peca(2, 5);

        // Act & Assert
        Assert.True(tabuleiro.PodeColar(peca, LadoTabuleiro.Esquerda));
        Assert.True(tabuleiro.PodeColar(peca, LadoTabuleiro.Direita));
    }

    /// <summary>
    /// <b>Objetivo:</b> Garantir que apenas peças compatíveis com a ponta do lado escolhido possam ser coladas.
    /// <br/><b>Critério:</b> PodeColar deve retornar true apenas se o valor da peça bate com a ponta.
    /// <br/><b>O que o aluno deve garantir:</b> A validação deve ser feita corretamente para ambos os lados.
    /// </summary>
    [Trait("Categoria", "Gap")]
    [Fact(DisplayName = "Deve permitir colar apenas peças compatíveis com a ponta do lado escolhido.")]
    public void Tabuleiro_PodeColar_DevePermitirApenasSeValorBateComPonta()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();
        tabuleiro.Pecas.Add(new Peca(2, 5));
        var pecaCompativel = new Peca(5, 3);
        var pecaIncompativel = new Peca(1, 4);

        // Act & Assert
        Assert.True(tabuleiro.PodeColar(pecaCompativel, LadoTabuleiro.Direita));
        Assert.False(tabuleiro.PodeColar(pecaIncompativel, LadoTabuleiro.Direita));
    }

    /// <summary>
    /// <b>Objetivo:</b> Garantir que peças sejam adicionadas no lado correto do tabuleiro, respeitando as regras do dominó.
    /// <br/><b>Critério:</b> Após sequência de colagens, as pontas devem refletir os valores esperados.
    /// <br/><b>O que o aluno deve garantir:</b> O método Colar deve manipular corretamente a ordem e inversão das peças.
    /// </summary>
    [Trait("Categoria", "Gap")]
    [Fact(DisplayName = "Deve adicionar peça no lado correto, respeitando regras do dominó.")]
    public void Tabuleiro_Colar_DeveAdicionarPecaNoLadoCorreto()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();
        var peca1 = new Peca(2, 5);
        var peca2 = new Peca(5, 3);

        // Act
        tabuleiro.Colar(peca1, LadoTabuleiro.Esquerda);
        tabuleiro.Colar(peca2, LadoTabuleiro.Direita);

        // Assert
        Assert.Equal(2, tabuleiro.Pecas[0].ValorA);
        Assert.Equal(3, tabuleiro.Pecas[1].ValorB);
    }

    /// <summary>
    /// <b>Objetivo:</b> Garantir que o método EstaTravado retorne true quando nenhuma mão puder jogar nas pontas.
    /// <br/><b>Critério:</b> EstaTravado deve retornar true se nenhuma mão tem peça compatível.
    /// <br/><b>O que o aluno deve garantir:</b> A lógica deve considerar todas as mãos e pontas do tabuleiro.
    /// </summary>
    [Trait("Categoria", "Gap")]
    [Fact(DisplayName = "Deve retornar true para travamento quando nenhuma mão pode jogar.")]
    public void Tabuleiro_EstaTravado_DeveRetornarTrueQuandoNenhumaMaoPodeJogar()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();
        tabuleiro.Pecas.Add(new Peca(2, 5));
        var mao1 = CriarMaoComPecas(new[] { new Peca(1, 1) });
        var mao2 = CriarMaoComPecas(new[] { new Peca(3, 4) });

        // Act & Assert
        Assert.True(tabuleiro.EstaTravado(new[] { mao1, mao2 }));
    }

    /// <summary>
    /// <b>Objetivo:</b> Garantir que o método EstaTravado retorne false se ao menos uma mão puder jogar nas pontas.
    /// <br/><b>Critério:</b> EstaTravado deve retornar false se alguma mão tem peça compatível.
    /// <br/><b>O que o aluno deve garantir:</b> A lógica deve considerar todas as mãos e pontas do tabuleiro.
    /// </summary>
    [Trait("Categoria", "Gap")]
    [Fact(DisplayName = "Deve retornar false para travamento se ao menos uma mão pode jogar.")]
    public void Tabuleiro_EstaTravado_DeveRetornarFalseSeAlgumaMaoPodeJogar()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();
        tabuleiro.Pecas.Add(new Peca(2, 5));
        var mao1 = CriarMaoComPecas(new[] { new Peca(5, 1) });
        var mao2 = CriarMaoComPecas(new[] { new Peca(3, 4) });

        // Act & Assert
        Assert.False(tabuleiro.EstaTravado(new[] { mao1, mao2 }));
    }

    /// <summary>
    /// <b>Objetivo:</b> Simula sequência de colagens e verifica se as pontas mudam corretamente após cada jogada.
    /// <br/><b>Critério:</b> Mudança de estado correta após cada jogada.
    /// <br/><b>O que o aluno deve garantir:</b> O método Colar deve manipular corretamente a ordem e inversão das peças.
    /// </summary>
    [Trait("Categoria", "Gap")]
    [Fact(DisplayName = "Deve permitir sequência de colagens com mudança de estado.")]
    public void Tabuleiro_Colar_DevePermitirSequenciaDeJogadasComMudancaDeEstado()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();
        var p1 = new Peca(6, 6);
        var p2 = new Peca(6, 5);
        var p3 = new Peca(5, 3);

        // Act
        tabuleiro.Colar(p1, LadoTabuleiro.Esquerda); // tabuleiro: [6|6]
        tabuleiro.Colar(p2, LadoTabuleiro.Direita); // tabuleiro: [6|6][6|5]
        tabuleiro.Colar(p3, LadoTabuleiro.Direita); // tabuleiro: [6|6][6|5][5|3]

        // Assert
        Assert.Equal(6, tabuleiro.PontaEsquerda);
        Assert.Equal(3, tabuleiro.PontaDireita);
    }

    /// <summary>
    /// <b>Objetivo:</b> Simula jogadas até travar o tabuleiro e depois destrava ao adicionar uma mão compatível.
    /// <br/><b>Critério:</b> EstaTravado deve mudar de true para false conforme o estado das mãos.
    /// <br/><b>O que o aluno deve garantir:</b> O método EstaTravado deve ser dinâmico e responder a mudanças nas mãos dos jogadores.
    /// </summary>
    [Trait("Categoria", "Gap")]
    [Fact(DisplayName = "Deve detectar travamento e destravamento conforme as mãos mudam.")]
    public void Tabuleiro_EstaTravado_DeveDetectarTravamentoAposJogadas()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();
        tabuleiro.Pecas.Add(new Peca(2, 5));
        tabuleiro.Pecas.Add(new Peca(5, 3));
        var mao1 = CriarMaoComPecas(new[] { new Peca(1, 1) });
        var mao2 = CriarMaoComPecas(new[] { new Peca(4, 4) });

        // Act
        var resultadoTravadoInicial = tabuleiro.EstaTravado(new[] { mao1, mao2 });
        var mao3 = CriarMaoComPecas(new[] { new Peca(3, 6) });
        var resultadoTravadoFinal = tabuleiro.EstaTravado(new[] { mao1, mao2, mao3 });

        // Assert
        Assert.True(resultadoTravadoInicial);
        Assert.False(resultadoTravadoFinal);
    }

    private MaoJogador CriarMaoComPecas(IEnumerable<Peca> pecas)
    {
        var mao = new MaoJogador(new Jogador("Teste"));
        foreach (var p in pecas) mao.AdicionarPeca(p);
        return mao;
    }

    #endregion
}
