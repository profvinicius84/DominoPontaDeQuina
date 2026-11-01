# Dominó Ponta de Quina

Projeto acadêmico com API de dominó individual, painel Blazor e cliente MAUI Windows.
Os alunos desenvolvem clientes usando os contratos públicos; as regras são executadas pela API.

## Estado atual

API, persistência, cliente HTTP com consulta periódica, painel e MAUI estão implementados. O webhook permanece disponível para compatibilidade com clientes anteriores.
A API usa SQL Server quando `Domino:UsarSqlite=false`; `/health` confirma a consulta ao banco.
A suíte automatizada cobre domínio, identidade, concorrência, contratos, componentes e jornada até encerramento.
O App Service de validação está publicado; o aceite operacional da consulta periódica ainda depende da interface MAUI com duas instâncias e de uma jornada autenticada no ambiente publicado.
Consulte a [matriz de aceite T-45](sdd/features/partida-individual-plataforma/execucao-t45.md).

## Acesso e fluxo

1. O cliente solicita um token com seu e-mail FIAP; a API cria a conta se ainda não existir.
2. O usuário recebe o token por e-mail. O token é reutilizável até revogação.
3. Cada cliente seleciona e ativa um jogador-perfil. Uma conta pode ter vários perfis; cada perfil fica ativo em um cliente.
4. O cliente cria uma partida ou lista as disponíveis e ingressa enviando seu perfil. A API monta a participação.
5. Dois perfis da mesma conta podem ocupar as duas posições. Um participante inicia a partida.
6. Cada ação retorna o estado confirmado. O cliente consulta atualizações por versão e aplica o novo estado quando houver mudança.

A API indica a pedra obrigatória de abertura, turno e próxima ação. Cada rodada começa com sete pedras por jogador. Sem encaixe, compra-se uma pedra; sem encaixe após a compra, passa-se o turno. Pontas múltiplas de cinco geram pontos. Veja as [regras completas](sdd/product-spec.md).

## Guias públicos

- [API: rotas, autenticação, atualizações e erros](DominoPontaDeQuina.Api/README.md). Swagger em `/swagger` e `/swagger/v1/swagger.json`.
- [Cliente HTTP e consulta de atualizações](DominoPontaDeQuina.Client/README.md).
- [Painel Blazor, somente projeção pública](DominoPontaDeQuina.Blazor/README.md).
- [MAUI Windows e roteiro de duas instâncias](DominoPontaDeQuina.Maui/README.md).
- [Relay HTTPS para clientes legados](DominoPontaDeQuina.WebhookRelay/README.md).
- [Migrations SQL Server e atualização SQLite](DominoPontaDeQuina.Migrations/README.md).

## Arquitetura

Core executa regras; Domain define entidades persistentes; Application coordena os casos de uso;
Infrastructure implementa persistência e e-mail. Api organiza as rotas HTTP em controllers e entrega a outbox de webhooks.
Client contém transporte e sincronização, consumidos pelo Blazor e MAUI sem acesso ao banco ou motor.
Migrations mantém o esquema; Tests cobre as camadas e contratos.
O projeto histórico Repository fica fora da solução principal e não é a persistência da API.

## Desenvolvimento e verificação

Projetos net8.0. Para testes, basta SDK .NET 8 ou compatível:

```powershell
dotnet restore DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj
dotnet build DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --configuration Release --no-restore
dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=tests.trx" --results-directory TestResults
```

Para a solução `.slnx`, use SDK compatível com esse formato (validação local com 10.0.401).
MAUI requer Windows e workload `maui-windows`, conforme seu guia:

```powershell
dotnet build DominoPontaDeQuina.slnx
dotnet build DominoPontaDeQuina.Windows.slnx
```

O [pipeline](.github/workflows/validate-tests.yml) executa restore, build Release e testes em Ubuntu/.NET 8,
sem tolerar falhas obrigatórias. Também compila Migrations e WebhookRelay e preserva resultados TRX mesmo
quando testes falham. O build dos testes inclui API, Blazor, cliente e camadas do servidor.
Não executa MAUI, envio pelo SendGrid nem migrations contra um banco operacional.
A reprodução local usa Windows; não equivale a uma execução remota aprovada do GitHub Actions.

## Persistência e configuração

SQL Server é o alvo de migrations; SQLite descartável é usado em testes e pode ser configurado
para desenvolvimento. EnsureCreated não atualiza bases antigas: siga o guia de migrations.
Configure conexões, SendGrid e segredo de webhook no `DominoPontaDeQuina.Api/appsettings.json` local ou por variáveis de ambiente; não versione credenciais.

O [plano](sdd/features/partida-individual-plataforma/tasks.md) e a matriz de aceite distinguem
implementação, testes locais e validações operacionais pendentes. Publicação para alunos depende
dessas evidências; build ou pipeline verde isoladamente não constitui aceite completo.
