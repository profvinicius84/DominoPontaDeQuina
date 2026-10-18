using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;

namespace DominoPontaDeQuina.Tests;

/// <summary>Valida invariantes estruturais do agregado persistente da partida.</summary>
public class EstadoPersistentePartidaTests
{
    [Fact(DisplayName = "Partida individual aceita somente dois participantes distintos.")]
    public void AdicionarParticipacao_LimitaPartidaAdoisJogadores()
    {
        var partida = new Partida();
        partida.AdicionarParticipacao(CriarParticipacao(1));
        partida.AdicionarParticipacao(CriarParticipacao(2));

        Assert.Throws<InvalidOperationException>(() =>
            partida.AdicionarParticipacao(CriarParticipacao(3)));

        Assert.Equal(2, partida.Participacoes.Count);
        Assert.All(partida.Participacoes, item => Assert.Equal(partida.Id, item.PartidaId));
    }

    [Fact(DisplayName = "Rodadas persistidas exigem numeração contínua na partida.")]
    public void AdicionarRodada_ExigeSequenciaContinua()
    {
        var partida = new Partida();
        partida.AdicionarRodada(new RodadaPartida { Numero = 1 });

        Assert.Throws<InvalidOperationException>(() =>
            partida.AdicionarRodada(new RodadaPartida { Numero = 3 }));

        Assert.Single(partida.Rodadas);
        Assert.Equal(partida.Id, partida.Rodadas.Single().PartidaId);
    }

    [Fact(DisplayName = "Ações persistidas preservam ordem do servidor e coerência de versão.")]
    public void RegistrarAcao_ExigeOrdemCrescenteEVersaoDeAcaoAceita()
    {
        var partida = new Partida();
        partida.RegistrarAcao(CriarAcao(partida.Id, ordem: 1, versaoEsperada: 4, versaoResultante: 5));

        Assert.Throws<InvalidOperationException>(() =>
            partida.RegistrarAcao(CriarAcao(partida.Id, ordem: 1, versaoEsperada: 5, versaoResultante: 6)));
        Assert.Throws<InvalidOperationException>(() =>
            partida.RegistrarAcao(CriarAcao(partida.Id, ordem: 2, versaoEsperada: 5, versaoResultante: 7)));

        Assert.Single(partida.Acoes);
        Assert.Equal(5, partida.Acoes.Single().VersaoResultante);
    }

    [Fact(DisplayName = "Ação rejeitada não persiste versão resultante.")]
    public void ValidarCoerencia_RejeicaoNaoPossuiVersaoResultante()
    {
        var acao = CriarAcao(Guid.NewGuid(), ordem: 1, versaoEsperada: 2, versaoResultante: 3);
        acao.FoiAceita = false;

        Assert.Throws<InvalidOperationException>(acao.ValidarCoerencia);
    }

    [Fact(DisplayName = "Contrato de estado expõe recuperação e idempotência por autor.")]
    public void IEstadoPartidaRepository_DeclaraLeiturasNecessariasAoAgregado()
    {
        var nomes = typeof(IEstadoPartidaRepository).GetMethods().Select(metodo => metodo.Name);

        Assert.Contains("ObterParaRecuperacaoAsync", nomes);
        Assert.Contains("ObterParaParticipanteAsync", nomes);
        Assert.Contains("ObterParaObservadorAsync", nomes);
        Assert.Contains("ObterPorChaveIdempotenciaAsync", nomes);
    }

    private static ParticipacaoPartida CriarParticipacao(int posicao) => new()
    {
        JogadorId = Guid.NewGuid(),
        Posicao = posicao
    };

    private static AcaoPartida CriarAcao(Guid partidaId, long ordem, long versaoEsperada, long? versaoResultante) => new()
    {
        PartidaId = partidaId,
        AutorJogadorId = Guid.NewGuid(),
        ChaveIdempotencia = Guid.NewGuid(),
        Tipo = TipoAcaoPartida.Jogada,
        OrdemRecebimento = ordem,
        VersaoEsperada = versaoEsperada,
        VersaoResultante = versaoResultante,
        FoiAceita = true,
        RecebidaEmUtc = DateTime.UtcNow
    };
}
