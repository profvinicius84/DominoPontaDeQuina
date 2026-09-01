     # Domino Ponta de Quina

Projeto de implementação das regras do jogo Dominó Ponta de Quina, organizado em camadas.

## Estrutura da solução

- `DominoPontaDeQuina.Core`: regras de negócio, modelos, interfaces e enums do jogo.
- `DominoPontaDeQuina.Domain`: entidades persistentes e relacionamentos do domínio.
- `DominoPontaDeQuina.Infrastructure`: persistência com Entity Framework Core e mapeamentos Fluent API.
- `DominoPontaDeQuina.Migrations`: projeto independente para migrations do banco de dados.
- `DominoPontaDeQuina.Tests`: testes automatizados do domínio.

## Persistência

O banco utilizado é SQL Server LocalDB. O `DominoDbContext` está em `DominoPontaDeQuina.Infrastructure` e possui as entidades:

- `Usuario`, utilizada para autenticação;
- `Partida`;
- `Jogador`;
- `ParticipacaoPartida`, associação entre jogador e partida com seus pontos;
- `Lance`, registro de lances com timestamp;
- `Ranking`, contador de vitórias por jogador.

As chaves estrangeiras ficam implícitas nas entidades e são configuradas como shadow properties pelo Fluent API.

## Dependências entre camadas

`Domain` não depende de nenhuma camada e contém as entidades e os contratos dos repositórios. `Application` depende apenas de `Domain` e implementa os casos de uso. `Infrastructure` depende de `Application` e `Domain` para implementar os repositórios com EF Core. A composição é feita por injeção de dependência.

### Configuração da conexão

A connection string padrão fica em `DominoPontaDeQuina.Migrations/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=DominoPontaDeQuina;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

Para sobrescrevê-la sem alterar o arquivo, utilize a variável `ConnectionStrings__DefaultConnection`.

### Migrations

A partir da raiz do repositório:

```bash
dotnet ef database update \
  --project DominoPontaDeQuina.Migrations \
  --startup-project DominoPontaDeQuina.Migrations \
  --context DominoDbContext
```

Para criar uma nova migration:

```bash
dotnet ef migrations add NomeDaMigration \
  --project DominoPontaDeQuina.Migrations \
  --startup-project DominoPontaDeQuina.Migrations \
  --context DominoDbContext \
  --output-dir Persistence/Migrations
```

## Build e testes

```bash
dotnet restore DominoPontaDeQuina.slnx
dotnet build DominoPontaDeQuina.slnx
dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj
```

O workflow do GitHub Actions executa restore, build e testes. SonarCloud, geração de relatórios e artefatos não fazem parte da pipeline atual. Falhas dos testes não bloqueiam o workflow.
