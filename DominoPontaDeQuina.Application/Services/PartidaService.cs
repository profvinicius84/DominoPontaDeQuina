using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;

namespace DominoPontaDeQuina.Application.Services;

/// <summary>Implementa os casos de uso definidos por <see cref="IPartidaService"/>.</summary>
public sealed class PartidaService(
    IPartidaRepository partidas,
    IJogadorRepository jogadores,
    IParticipacaoRepository participacoes,
    ILanceRepository lances,
    IRankingRepository ranking) : IPartidaService
{
    /// <inheritdoc />
    public Task<Partida> IniciarPartidaAsync(int pontuacaoAlvo = 50, CancellationToken cancellationToken = default)
    {
        if (pontuacaoAlvo <= 0)
            throw new ArgumentOutOfRangeException(nameof(pontuacaoAlvo));

        var partida = new Partida
        {
            Id = Guid.NewGuid(),
            PontuacaoAlvo = pontuacaoAlvo,
            Status = "EmAndamento",
            CriadaEm = DateTime.UtcNow
        };
        return partidas.AdicionarAsync(partida, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Jogador> RegistrarJogadorAsync(Guid partidaId, string nome, Guid? usuarioId = null, CancellationToken cancellationToken = default)
    {
        ValidarNome(nome);
        await ObterPartidaOuFalharAsync(partidaId, cancellationToken);

        var jogador = await jogadores.AdicionarAsync(new Jogador
        {
            Id = Guid.NewGuid(),
            Nome = nome
        }, usuarioId, cancellationToken);
        await participacoes.AdicionarAsync(new ParticipacaoPartida { Pontos = 0 }, partidaId, jogador.Id, cancellationToken);
        return jogador;
    }

    /// <inheritdoc />
    public async Task<Lance> RegistrarLanceAsync(Guid partidaId, Guid jogadorId, CancellationToken cancellationToken = default)
    {
        var partida = await ObterPartidaOuFalharAsync(partidaId, cancellationToken);
        if (!string.Equals(partida.Status, "EmAndamento", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A partida não está em andamento.");
        if (!await jogadores.ExisteAsync(jogadorId, cancellationToken))
            throw new KeyNotFoundException("Jogador não encontrado.");

        return await lances.AdicionarAsync(new Lance
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow
        }, partidaId, jogadorId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Partida> VerificarStatusAsync(Guid partidaId, CancellationToken cancellationToken = default) =>
        ObterPartidaOuFalharAsync(partidaId, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Ranking>> ConsultarRankingAsync(CancellationToken cancellationToken = default) =>
        ranking.ObterAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Partida>> ConsultarHistoricoAsync(CancellationToken cancellationToken = default) =>
        partidas.ObterHistoricoAsync(cancellationToken);

    async Task<Partida> ObterPartidaOuFalharAsync(Guid id, CancellationToken cancellationToken)
    {
        return await partidas.ObterAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Partida não encontrada.");
    }

    static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do jogador é obrigatório.", nameof(nome));
    }
}
