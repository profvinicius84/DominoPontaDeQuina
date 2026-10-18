using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Infrastructure.Persistence.Repositories;

/// <summary>Recupera o agregado persistido sem transformar entidades em contratos públicos.</summary>
public sealed class EstadoPartidaRepository(DominoDbContext db) : IEstadoPartidaRepository
{
    public async Task<IReadOnlyList<Partida>> ListarDisponiveisAsync(CancellationToken cancellationToken = default) =>
        await db.Partidas.AsNoTracking().Where(p => p.Status == "AguardandoInicio" && p.Participacoes.Count == 1)
            .OrderBy(p => p.CriadaEm).ThenBy(p => p.Id).ToListAsync(cancellationToken);

    public Task<bool> PossuiParticipacaoNaoEncerradaAsync(Guid jogadorId, Guid? excetoPartidaId = null, CancellationToken cancellationToken = default) =>
        db.ParticipacoesPartida.AsNoTracking().AnyAsync(p => p.JogadorId == jogadorId
            && p.Partida.Status != "Encerrada" && (!excetoPartidaId.HasValue || p.PartidaId != excetoPartidaId.Value), cancellationToken);
    /// <inheritdoc />
    public Task<Partida?> ObterParaRecuperacaoAsync(Guid partidaId, CancellationToken cancellationToken = default) =>
        CriarConsultaBase()
            .Include(partida => partida.Participacoes).ThenInclude(participacao => participacao.Mao)
            .Include(partida => partida.Rodadas.OrderBy(rodada => rodada.Numero)).ThenInclude(rodada => rodada.Cava.OrderBy(pedra => pedra.Ordem))
            .Include(partida => partida.Rodadas.OrderBy(rodada => rodada.Numero))
                .ThenInclude(rodada => rodada.Acoes.OrderBy(acao => acao.OrdemRecebimento))
            .Include(partida => partida.Acoes.OrderBy(acao => acao.OrdemRecebimento))
            .SingleOrDefaultAsync(partida => partida.Id == partidaId, cancellationToken);

    /// <inheritdoc />
    public Task<Partida?> ObterParaAtualizacaoAsync(Guid partidaId, CancellationToken cancellationToken = default) =>
        db.Partidas
            .Include(partida => partida.Participacoes).ThenInclude(participacao => participacao.Jogador)
            .Include(partida => partida.Participacoes).ThenInclude(participacao => participacao.Mao)
            .Include(partida => partida.Rodadas.OrderBy(rodada => rodada.Numero)).ThenInclude(rodada => rodada.Cava.OrderBy(pedra => pedra.Ordem))
            .Include(partida => partida.Rodadas.OrderBy(rodada => rodada.Numero))
                .ThenInclude(rodada => rodada.Acoes.OrderBy(acao => acao.OrdemRecebimento))
            .Include(partida => partida.Acoes.OrderBy(acao => acao.OrdemRecebimento))
            .SingleOrDefaultAsync(partida => partida.Id == partidaId, cancellationToken);

    /// <inheritdoc />
    public async Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default)
    {
        db.Partidas.Add(partida);
        if (!await TentarSalvarAsync(cancellationToken)) throw new ConflitoParticipacaoException();
    }

    /// <inheritdoc />
    public async Task<bool> TentarSalvarAsync(CancellationToken cancellationToken = default)
    {
        var versoes = db.ChangeTracker.Entries<Partida>().Where(e => e.State == EntityState.Modified)
            .Select(e => (e.Entity.Id, Versao: e.OriginalValues.GetValue<long>(nameof(Partida.VersaoEstado)))).ToArray();
        try
        {
            await using var transacao = await db.Database.BeginTransactionAsync(cancellationToken);
            var novas = db.ChangeTracker.Entries<ParticipacaoPartida>().Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity).ToArray();
            var autorizados = db.ChangeTracker.Entries<Jogador>().Where(e => e.Property(j => j.AtivacaoHash).IsModified)
                .ToDictionary(e => e.Entity.Id, e => e.Property(j => j.AtivacaoHash).OriginalValue);
            foreach (var jogadorId in novas.Select(p => p.JogadorId).Concat(autorizados.Keys).Distinct().Order())
            {
                var consulta = db.Jogadores.Where(j => j.Id == jogadorId);
                if (autorizados.TryGetValue(jogadorId, out var hash)) consulta = consulta.Where(j => j.AtivacaoHash == hash);
                // Reserva a linha do perfil até o commit; outros ingressos e liberações aguardam.
                if (await consulta.ExecuteUpdateAsync(s => s.SetProperty(j => j.Nome, j => j.Nome), cancellationToken) != 1)
                { db.ChangeTracker.Clear(); return false; }
            }
            foreach (var nova in novas)
                if (await PossuiParticipacaoNaoEncerradaAsync(nova.JogadorId, nova.PartidaId, cancellationToken))
                { db.ChangeTracker.Clear(); return false; }
            await db.SaveChangesAsync(cancellationToken);
            await transacao.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return false;
        }
        catch (DbUpdateException)
        {
            // O índice de posição/idempotência também pode detectar a disputa antes do UPDATE da versão.
            db.ChangeTracker.Clear();
            foreach (var versao in versoes)
                if (await db.Partidas.AsNoTracking().AnyAsync(p => p.Id == versao.Id && p.VersaoEstado != versao.Versao, cancellationToken))
                    return false;
            // Sem evidência de outra transição, preservar a falha real de persistência.
            throw;
        }
    }

    /// <inheritdoc />
    public void DescartarAlteracoes() => db.ChangeTracker.Clear();

    /// <inheritdoc />
    public void AdicionarAoContexto(object entidade) => db.Add(entidade);

    /// <inheritdoc />
    public Task<AcaoPartida?> ObterPorChaveIdempotenciaAsync(
        Guid partidaId,
        Guid autorJogadorId,
        Guid chaveIdempotencia,
        CancellationToken cancellationToken = default) =>
        db.AcoesPartida.AsNoTracking().SingleOrDefaultAsync(acao =>
            acao.PartidaId == partidaId
            && acao.AutorJogadorId == autorJogadorId
            && acao.ChaveIdempotencia == chaveIdempotencia,
            cancellationToken);

    /// <inheritdoc />
    public async Task<Partida?> ObterParaParticipanteAsync(
        Guid partidaId,
        Guid jogadorId,
        CancellationToken cancellationToken = default)
    {
        var partida = await CriarConsultaBase()
            .Include(item => item.Participacoes).ThenInclude(participacao => participacao.Jogador)
            .Include(item => item.Rodadas.OrderBy(rodada => rodada.Numero))
                .ThenInclude(rodada => rodada.Cava.OrderBy(pedra => pedra.Ordem))
            .Include(item => item.Rodadas.OrderBy(rodada => rodada.Numero))
                .ThenInclude(rodada => rodada.Acoes.OrderBy(acao => acao.OrdemRecebimento))
            .Include(item => item.Acoes.OrderBy(acao => acao.OrdemRecebimento))
            .SingleOrDefaultAsync(item => item.Id == partidaId && item.Participacoes.Any(participacao => participacao.JogadorId == jogadorId), cancellationToken);

        if (partida is null)
            return null;

        var participacao = partida.Participacoes.Single(participacao => participacao.JogadorId == jogadorId);
        participacao.Mao = await db.PedrasMaoPartida.AsNoTracking()
            .Where(pedra => pedra.PartidaId == partidaId && pedra.JogadorId == jogadorId)
            .OrderBy(pedra => pedra.ValorA).ThenBy(pedra => pedra.ValorB)
            .ToListAsync(cancellationToken);
        return partida;
    }

    /// <inheritdoc />
    public Task<Partida?> ObterParaObservadorAsync(Guid partidaId, CancellationToken cancellationToken = default) =>
        CriarConsultaBase()
            .Include(partida => partida.Participacoes).ThenInclude(participacao => participacao.Jogador)
            .Include(partida => partida.Rodadas.OrderBy(rodada => rodada.Numero))
                .ThenInclude(rodada => rodada.Cava.OrderBy(pedra => pedra.Ordem))
            .Include(partida => partida.Rodadas.OrderBy(rodada => rodada.Numero))
                .ThenInclude(rodada => rodada.Acoes.OrderBy(acao => acao.OrdemRecebimento))
            .Include(partida => partida.Acoes.OrderBy(acao => acao.OrdemRecebimento))
            .SingleOrDefaultAsync(partida => partida.Id == partidaId, cancellationToken);

    private IQueryable<Partida> CriarConsultaBase() => db.Partidas.AsNoTracking();
}
