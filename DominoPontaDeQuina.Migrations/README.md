# Migrations

## Abertura obrigatória e outbox de webhook — T-36 a T-39

`20260923171528_FluxoClientesWebhookConfiavel` adiciona `ParticipacoesPartida.Id`, as faces da pedra de abertura em `RodadasPartida` e `EventosWebhook`, com índices para unicidade por participação/versão e busca de tentativas pendentes. IDs das participações anteriores são preenchidos com `NEWID()` antes do índice único.

A cadeia completa foi aplicada desde uma base vazia no SQL Server descartável `DominoPontaDeQuina_T39_20260923_142500`, incluindo a migration de perfis anterior, e terminou com `Done.`. A migration final também foi aplicada sobre o banco descartável T-34 já atualizado. Nenhum banco de uso foi alterado. Bases SQLite novas recebem o modelo por `EnsureCreated`; a atualização de arquivo SQLite legado para esta etapa ainda deve ser feita com backup e recriação controlada antes de uso.

## Perfis, ativação e registro de webhook — 2026-09-23

`20260923164646_PerfisJogadorAtivacaoWebhook` acrescenta `AtivacaoHash`/`AtivadoEmUtc` a `Jogadores` e `WebhookUrl` a `ParticipacoesPartida`. Todos são opcionais para preservar dados anteriores. `UsuarioId` torna-se propriedade explícita do modelo mantendo a coluna e a relação existentes; um usuário já pode possuir vários jogadores no banco.

Aplicada com sucesso no banco descartável `DominoPontaDeQuina_T34_20260923_131940`, após a migration de tokens, sem modificar bases de uso. Para SQLite legado, com backup e API parada, execute uma vez [Sqlite/PerfisJogadorAtivacaoWebhook.sql](Sqlite/PerfisJogadorAtivacaoWebhook.sql) depois da atualização de tokens, caso ainda não possua as colunas. Não execute os scripts de upgrade em bases novas criadas pelo modelo atual.

## Tokens reutilizáveis até revogação — T-34

`20260923131940_TokensReutilizaveisAteRevogacao` cria `TokensAcesso` com hash único, data de emissão e revogação opcional. Não existe data de expiração. A tabela ainda não estava no histórico de migrations anterior; por isso a migration cria a tabela, em vez de alterar uma coluna existente. O snapshot foi atualizado.

A migration foi aplicada após `InitialCreate` e `EstadoPartidaRecuperavel` no banco descartável `DominoPontaDeQuina_T34_20260923_131940`, em SQL Server local, em 2026-09-23. O banco principal não foi alterado. Comando reproduzível:

```powershell
dotnet ef database update --no-build --project DominoPontaDeQuina.Migrations --connection "Server=localhost;Database=DominoPontaDeQuina_T34_20260923_131940;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False"
```

Bases SQLite de desenvolvimento criadas pela versão anterior com `EnsureCreated` não são atualizadas automaticamente. Com a API parada e backup do arquivo, execute uma única vez [Sqlite/TokensReutilizaveis.sql](Sqlite/TokensReutilizaveis.sql) usando SQLite 3.35 ou superior. O script preserva usuários e tokens, remove a coluna antiga e mantém revogados os tokens já revogados ou expirados na data da atualização. Novos tokens e tokens ainda válidos passam a durar até revogação. Bases novas já recebem o esquema correto por `EnsureCreated` e não devem executar esse script. O upgrade é coberto por teste em banco descartável; não foi executado contra arquivos de desenvolvimento do usuário.

Este projeto concentra as migrations e referencia `DominoPontaDeQuina.Infrastructure`, que contém o `DominoDbContext`. Há uma única factory de design-time em `Persistence/DominoDbContextFactory.cs`; ela carrega o `appsettings.json` copiado para o diretório de saída do projeto. O arquivo versionado aponta para LocalDB, sem credenciais. Para aplicar migrations em outro SQL Server, informe `ConnectionStrings__DefaultConnection` ou `--connection` no comando de atualização. A conexão da API fica no seu `appsettings.json` local, ignorado pelo Git.

Para criar uma migration:

```bash
dotnet ef migrations add NomeDaMigration \
  --project DominoPontaDeQuina.Migrations \
  --startup-project DominoPontaDeQuina.Migrations \
  --context DominoDbContext \
  --output-dir Persistence/Migrations
```

Para aplicar o banco SQL Server LocalDB definido pela factory:

```bash
dotnet ef database update \
  --project DominoPontaDeQuina.Migrations \
  --startup-project DominoPontaDeQuina.Migrations \
  --context DominoDbContext
```

## Migration `EstadoPartidaRecuperavel`

`20260921201546_EstadoPartidaRecuperavel` adiciona o estado necessário para
retomar uma partida: versão, participantes posicionados, rodadas, ações,
mãos privadas e cava ordenada. Para bases já existentes, a migration atribui
as posições históricas de cada partida de forma determinística antes de criar
a restrição única de posição.

Antes de aplicar, valide em um banco descartável e nunca no banco compartilhado:

```powershell
dotnet ef database update `
  --project DominoPontaDeQuina.Migrations `
  --startup-project DominoPontaDeQuina.Migrations `
  --context DominoPontaDeQuina.Infrastructure.Persistence.DominoDbContext `
  --connection "Server=localhost;Database=DominoPontaDeQuina_T18_<sufixo>;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False"
```

Em 2026-09-21, a migration e o snapshot foram gerados e a listagem sem conexão
e o script idempotente foram produzidos. A aplicação não foi certificada neste
ambiente: LocalDB não inicializou e a instância SQL Server local recusou a
autenticação integrada com `Failed to generate SSPI context`. Use uma conta SQL
Server autorizada ou um ambiente descartável configurado antes de promover a
migration.

## Validação posterior da T-18 — 2026-09-21

A autenticação integrada funcionou ao executar fora do isolamento de processo.
Foi aplicado, com sucesso, o seguinte comando exclusivamente no banco descartável:

```powershell
dotnet ef database update --project DominoPontaDeQuina.Migrations --startup-project DominoPontaDeQuina.Migrations --context DominoPontaDeQuina.Infrastructure.Persistence.DominoDbContext --connection "Server=localhost;Database=DominoPontaDeQuina_T18_20260921_Implementacoes;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False"
```

Resultado: `InitialCreate` e `EstadoPartidaRecuperavel` aplicadas, `Done.`. SQL Server
2022 Developer 16.0.1000.6 local. O banco de validação foi preservado. A ferramenta
EF 8.0.8 ainda emite aviso de versão inferior ao runtime 8.0.19.
