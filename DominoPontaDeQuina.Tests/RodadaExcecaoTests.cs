using DominoPontaDeQuina.Core.Models;

namespace DominoPontaDeQuina.Core.Tests;

/// <summary>
/// Testes da classe Rodada para cenários de exceção.
/// </summary>
public class RodadaExcecaoTests
{
    /// <summary>
    /// <b>Objetivo:</b> Garantir exceção ao registrar jogada nula na rodada.
    /// <br/><b>Critério:</b> Deve lançar exceção e o tipo deve pertencer ao namespace do projeto.
    /// </summary>
    [Trait("Categoria", "Excecao")]
    [Fact(DisplayName = "Deve lançar exceção do projeto ao registrar jogada nula.")]
    public void RegistrarJogada_DeveLancarExcecao_QuandoJogadaForNula()
    {
        var rodada = new Rodada();

        Assert.Throws<ArgumentNullException>(() => rodada.RegistrarJogada(null!));
    }
}
