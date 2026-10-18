using DominoPontaDeQuina.Application.Identidade;
using DominoPontaDeQuina.Application.Identidade.Contracts;
using DominoPontaDeQuina.Infrastructure.Persistence;
using DominoPontaDeQuina.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Tests;

/// <summary>Valida elegibilidade FIAP, emissão sem exposição e ciclo de vida do token.</summary>
public sealed class IdentidadeAcessoIntegrationTests : IAsyncLifetime
{
    private const string SenhaAcademica = "3ESC#-CP6";
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        await using var db = CriarContexto();
        await db.Database.EnsureCreatedAsync();
    }
    public Task DisposeAsync() => connection.DisposeAsync().AsTask();

    [Fact(DisplayName = "Cadastro FIAP normaliza e autentica apenas a senha acadêmica fixa")]
    public async Task CadastroEAutenticacao_RestringemDominioESenha()
    {
        await using var db = CriarContexto();
        var entrega = new EntregaCapturada();
        var servico = CriarServico(db, entrega);
        var cadastro = await servico.CadastrarUsuarioAutorizadoAsync("  ALUNO@FIAP.COM.BR ");
        Assert.True(cadastro.Sucesso);
        Assert.False((await servico.CadastrarUsuarioAutorizadoAsync("aluno@example.com")).Sucesso);
        Assert.True((await servico.AutenticarAsync("aluno@fiap.com.br", SenhaAcademica)).Sucesso);
        Assert.False((await servico.AutenticarAsync("aluno@fiap.com.br", "senha-incorreta")).Sucesso);
    }

    [Fact(DisplayName = "Emissão é uniforme, entrega somente pelo adaptador e revogação impede reuso")]
    public async Task Token_EntregaValidaRevogaESemReativar()
    {
        Guid usuarioId;
        string token;
        await using (var db = CriarContexto())
        {
            var entrega = new EntregaCapturada();
            var servico = CriarServico(db, entrega);
            usuarioId = (await servico.CadastrarUsuarioAutorizadoAsync("aluno@fiap.com.br")).UsuarioId!.Value;
            var respostaElegivel = await servico.SolicitarTokenAsync("ALUNO@FIAP.COM.BR", SenhaAcademica);
            var respostaNaoElegivel = await servico.SolicitarTokenAsync("nao@fiap.com.br", SenhaAcademica);
            Assert.Equal(respostaElegivel, respostaNaoElegivel);
            token = Assert.Single(entrega.Entregas).Token;
            Assert.Equal(usuarioId, await servico.ValidarTokenAsync(token));
            Assert.True(await servico.RevogarTokenAsync(usuarioId, token));
            Assert.Null(await servico.ValidarTokenAsync(token));
        }

        await using (var db = CriarContexto())
        {
            var entrega = new EntregaCapturada();
            var servico = CriarServico(db, entrega);
            await servico.SolicitarTokenAsync("aluno@fiap.com.br", SenhaAcademica);
            var novoToken = Assert.Single(entrega.Entregas).Token;
            var persistido = await db.TokensAcesso.SingleAsync(item => item.RevogadoEmUtc == null);
            persistido.EmitidoEmUtc = DateTime.UtcNow.AddYears(-10);
            await db.SaveChangesAsync();
            Assert.Equal(usuarioId, await servico.ValidarTokenAsync(novoToken));
            Assert.Null(await servico.ValidarTokenAsync(token));
        }
    }

    [Fact(DisplayName = "Solicitação por e-mail cria a conta FIAP ausente e entrega o token")]
    public async Task SolicitarPorEmail_CadastraContaAusente()
    {
        await using var db = CriarContexto();
        var entrega = new EntregaCapturada();
        var servico = CriarServico(db, entrega);

        await servico.SolicitarTokenPorEmailAsync("  NOVO@FIAP.COM.BR ");
        await servico.SolicitarTokenPorEmailAsync("novo@fiap.com.br");
        await servico.SolicitarTokenPorEmailAsync("fora@example.com");

        Assert.Single(await db.Usuarios.ToListAsync());
        Assert.Equal(2, entrega.Entregas.Count);
        Assert.All(entrega.Entregas, item => Assert.Equal("novo@fiap.com.br", item.Email));
        Assert.Equal((await db.Usuarios.SingleAsync()).Id, await servico.ValidarTokenAsync(entrega.Entregas[0].Token));
    }

    [Fact(DisplayName = "Token antigo permanece válido após novos acessos e emissão; revogação é individual")]
    public async Task Token_ReutilizavelEntreContextosENovaEmissao()
    {
        var entrega = new EntregaCapturada();
        Guid usuarioId;
        string primeiro;
        await using (var db = CriarContexto())
        {
            var servico = CriarServico(db, entrega);
            usuarioId = (await servico.CadastrarUsuarioAutorizadoAsync("perfil@fiap.com.br")).UsuarioId!.Value;
            await servico.SolicitarTokenAsync("perfil@fiap.com.br", SenhaAcademica);
            primeiro = Assert.Single(entrega.Entregas).Token;
            var persistido = await db.TokensAcesso.SingleAsync();
            Assert.NotEqual(primeiro, persistido.Hash);
            persistido.EmitidoEmUtc = DateTime.UtcNow.AddYears(-10);
            await db.SaveChangesAsync();
        }

        // Novos contextos representam acessos posteriores sem estado de autenticação em memória.
        for (var acesso = 0; acesso < 3; acesso++)
        {
            await using var db = CriarContexto();
            Assert.Equal(usuarioId, await CriarServico(db, entrega).ValidarTokenAsync(primeiro));
        }

        await using (var db = CriarContexto())
        {
            var servico = CriarServico(db, entrega);
            await servico.SolicitarTokenAsync("perfil@fiap.com.br", SenhaAcademica);
            var segundo = entrega.Entregas[1].Token;
            Assert.NotEqual(primeiro, segundo);
            Assert.Equal(usuarioId, await servico.ValidarTokenAsync(primeiro));
            Assert.Equal(usuarioId, await servico.ValidarTokenAsync(segundo));
            Assert.False(await servico.RevogarTokenAsync(Guid.NewGuid(), primeiro));
            Assert.Equal(usuarioId, await servico.ValidarTokenAsync(primeiro));
            Assert.True(await servico.RevogarTokenAsync(usuarioId, primeiro));
            Assert.Null(await servico.ValidarTokenAsync(primeiro));
            Assert.Equal(usuarioId, await servico.ValidarTokenAsync(segundo));
            Assert.True(await servico.RevogarTokenAsync(usuarioId, segundo));
            Assert.Null(await servico.ValidarTokenAsync(segundo));
            Assert.False(await servico.RevogarTokenAsync(usuarioId, segundo));
            Assert.Null(await servico.ValidarTokenAsync("token-invalido"));
            Assert.Null(await servico.ValidarTokenAsync(string.Empty));
        }
    }

    [Fact(DisplayName = "Upgrade SQLite preserva tokens ativos e revogados sem reativar os já expirados")]
    public async Task UpgradeSqlite_PreservaCredenciaisSemExpiracao()
    {
        var entrega = new EntregaCapturada();
        Guid usuarioId;
        await using (var db = CriarContexto())
        {
            var servico = CriarServico(db, entrega);
            usuarioId = (await servico.CadastrarUsuarioAutorizadoAsync("upgrade@fiap.com.br")).UsuarioId!.Value;
            for (var indice = 0; indice < 3; indice++)
                await servico.SolicitarTokenAsync("upgrade@fiap.com.br", SenhaAcademica);
            await servico.RevogarTokenAsync(usuarioId, entrega.Entregas[2].Token);
            // Reproduz o esquema anterior no banco descartável, com um token expirado.
            await db.Database.ExecuteSqlRawAsync("DROP INDEX IX_TokensAcesso_UsuarioId_RevogadoEmUtc;");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE TokensAcesso ADD COLUMN ExpiraEmUtc TEXT NOT NULL DEFAULT '2999-01-01 00:00:00';");
            await db.Database.ExecuteSqlRawAsync("CREATE INDEX IX_TokensAcesso_UsuarioId_ExpiraEmUtc ON TokensAcesso (UsuarioId, ExpiraEmUtc);");
            var expiradoHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(entrega.Entregas[1].Token)));
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE TokensAcesso SET ExpiraEmUtc = '2000-01-01 00:00:00' WHERE Hash = {expiradoHash}");
            var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sqlite", "TokensReutilizaveis.sql"));
            await db.Database.ExecuteSqlRawAsync(script);
        }
        await using (var db = CriarContexto())
        {
            var servico = CriarServico(db, entrega);
            Assert.Equal(3, await db.TokensAcesso.CountAsync());
            Assert.Equal(usuarioId, await servico.ValidarTokenAsync(entrega.Entregas[0].Token));
            Assert.Null(await servico.ValidarTokenAsync(entrega.Entregas[1].Token));
            Assert.Null(await servico.ValidarTokenAsync(entrega.Entregas[2].Token));
            await servico.SolicitarTokenAsync("upgrade@fiap.com.br", SenhaAcademica);
            Assert.Equal(usuarioId, await servico.ValidarTokenAsync(entrega.Entregas[3].Token));
            Assert.Equal(usuarioId, await servico.ValidarTokenAsync(entrega.Entregas[0].Token));
        }
    }

    private IdentidadeAcessoService CriarServico(DominoDbContext db, IEntregaTokenEmail entrega) => new(new IdentidadeRepository(db), entrega);
    private DominoDbContext CriarContexto() => new(new DbContextOptionsBuilder<DominoDbContext>().UseSqlite(connection).Options);

    private sealed class EntregaCapturada : IEntregaTokenEmail
    {
        public List<(string Email, string Token)> Entregas { get; } = [];
        public Task EntregarAsync(string emailNormalizado, string token, CancellationToken cancellationToken = default)
        {
            Entregas.Add((emailNormalizado, token));
            return Task.CompletedTask;
        }
    }
}
