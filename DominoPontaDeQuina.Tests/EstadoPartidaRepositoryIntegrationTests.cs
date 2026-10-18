using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Infrastructure.Persistence;
using DominoPontaDeQuina.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Tests;

/// <summary>Exercita as consultas EF do agregado contra SQLite descartável em memória.</summary>
public class EstadoPartidaRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        await using var db = CriarContexto();
        await db.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => connection.DisposeAsync().AsTask();

    [Fact(DisplayName = "Recuperação EF grava e lê rodadas e ações na ordem do servidor.")]
    public async Task ObterParaRecuperacaoAsync_PreservaOrdemDeRodadaEAcao()
    {
        var dados = await SemearAsync();
        await using var db = CriarContexto();
        var repositorio = new EstadoPartidaRepository(db);

        var partida = await repositorio.ObterParaRecuperacaoAsync(dados.PartidaId);

        Assert.NotNull(partida);
        Assert.Equal(new[] { 1, 2 }, partida!.Rodadas.Select(rodada => rodada.Numero));
        Assert.Equal(new long[] { 1, 2 }, partida.Acoes.Select(acao => acao.OrdemRecebimento));
        Assert.Equal(new long[] { 1, 2 }, partida.Rodadas.First().Acoes.Select(acao => acao.OrdemRecebimento));
        Assert.Equal(2, partida.Participacoes.Sum(participacao => participacao.Mao.Count));
    }

    [Fact(DisplayName = "Visão EF de participante e observador não carrega mão adversária.")]
    public async Task ConsultasDeVisao_SeparamMaosPrivadas()
    {
        var dados = await SemearAsync();

        await using (var dbParticipante = CriarContexto())
        {
            var participante = await new EstadoPartidaRepository(dbParticipante)
                .ObterParaParticipanteAsync(dados.PartidaId, dados.JogadorAId);

            Assert.NotNull(participante);
            Assert.Equal(2, participante!.Participacoes.Count);
            Assert.Single(participante.Participacoes.Single(item => item.JogadorId == dados.JogadorAId).Mao);
            Assert.Empty(participante.Participacoes.Single(item => item.JogadorId == dados.JogadorBId).Mao);
        }

        await using var dbObservador = CriarContexto();
        var observador = await new EstadoPartidaRepository(dbObservador).ObterParaObservadorAsync(dados.PartidaId);

        Assert.NotNull(observador);
        Assert.All(observador!.Participacoes, participacao => Assert.Empty(participacao.Mao));
    }

    private DominoDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<DominoDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;
        return new DominoDbContext(options);
    }

    [Fact]
    public async Task LiberacaoAposAutorizacao_ImpedeCommitEEventoComChaveAntiga()
    {
        var dados = await SemearAsync();
        await using var comando = CriarContexto();
        var jogador = await comando.Jogadores.SingleAsync(j => j.Id == dados.JogadorAId);
        jogador.AtivacaoHash = new string('a', 64);
        await comando.SaveChangesAsync();
        comando.Entry(jogador).Property(j => j.AtivacaoHash).IsModified = true;
        var partida = await comando.Partidas.SingleAsync(p => p.Id == dados.PartidaId);
        partida.VersaoEstado++;
        comando.Add(new EventoWebhook { PartidaId = partida.Id, JogadorId = jogador.Id,
            VersaoEstado = 3, Tipo = "estado-alterado", Destino = "https://cliente.example/webhook", ConteudoJson = "{}" });
        await using (var outro = CriarContexto())
            await outro.Jogadores.Where(j => j.Id == jogador.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(j => j.AtivacaoHash, (string?)null));

        Assert.False(await new EstadoPartidaRepository(comando).TentarSalvarAsync());
        await using var consulta = CriarContexto();
        Assert.Equal(2, (await consulta.Partidas.SingleAsync(p => p.Id == dados.PartidaId)).VersaoEstado);
        Assert.Empty(await consulta.Set<EventoWebhook>().ToListAsync());
        Assert.Null((await consulta.Jogadores.SingleAsync(j => j.Id == dados.JogadorAId)).AtivacaoHash);
    }

    private async Task<(Guid PartidaId, Guid JogadorAId, Guid JogadorBId)> SemearAsync()
    {
        var partidaId = Guid.NewGuid();
        var jogadorAId = Guid.NewGuid();
        var jogadorBId = Guid.NewGuid();
        var rodadaUmId = Guid.NewGuid();

        await using var db = CriarContexto();
        db.AddRange(
            new Jogador { Id = jogadorAId, Nome = "Alice" },
            new Jogador { Id = jogadorBId, Nome = "Bob" },
            new Partida { Id = partidaId, PontuacaoAlvo = 50, Status = "EmAndamento", VersaoEstado = 2 },
            new ParticipacaoPartida { PartidaId = partidaId, JogadorId = jogadorAId, Posicao = 1, Pontos = 5 },
            new ParticipacaoPartida { PartidaId = partidaId, JogadorId = jogadorBId, Posicao = 2, Pontos = 0 },
            new PedraMaoPartida { PartidaId = partidaId, JogadorId = jogadorAId, ValorA = 6, ValorB = 1 },
            new PedraMaoPartida { PartidaId = partidaId, JogadorId = jogadorBId, ValorA = 5, ValorB = 0 },
            new RodadaPartida { Id = rodadaUmId, PartidaId = partidaId, Numero = 1, Situacao = SituacaoRodadaPartida.EmAndamento },
            new RodadaPartida { PartidaId = partidaId, Numero = 2, Situacao = SituacaoRodadaPartida.AguardandoInicio },
            CriarAcao(partidaId, jogadorAId, rodadaUmId, ordem: 2, versaoEsperada: 1, versaoResultante: 2),
            CriarAcao(partidaId, jogadorBId, rodadaUmId, ordem: 1, versaoEsperada: 0, versaoResultante: 1));
        await db.SaveChangesAsync();
        return (partidaId, jogadorAId, jogadorBId);
    }

    private static AcaoPartida CriarAcao(
        Guid partidaId,
        Guid jogadorId,
        Guid rodadaId,
        long ordem,
        long versaoEsperada,
        long versaoResultante) => new()
        {
            PartidaId = partidaId,
            AutorJogadorId = jogadorId,
            RodadaPartidaId = rodadaId,
            Tipo = TipoAcaoPartida.Jogada,
            ChaveIdempotencia = Guid.NewGuid(),
            OrdemRecebimento = ordem,
            VersaoEsperada = versaoEsperada,
            VersaoResultante = versaoResultante,
            RecebidaEmUtc = DateTime.UtcNow.AddTicks(ordem),
            FoiAceita = true
        };
}
