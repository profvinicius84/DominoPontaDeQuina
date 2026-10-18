# Execução T-34 — token reutilizável até revogação

Data: 2026-09-23. Estado: implementação e validação local concluídas. Critério: AC-ID-03; suporte de autenticação compartilhada para AC-CLI-01, sem declarar implementadas as participações por perfil.

## Alterações

- `IdentidadeAcessoService`, `TokenAcesso`, `IIdentidadeRepository`, repositório e mapeamento EF: removida expiração; nova emissão preserva tokens anteriores; validação consulta hash e ausência de revogação.
- Revogação continua vinculada ao usuário e ao hash do token. Uma conta não revoga o token de outra. Revogar um token bloqueia todos os clientes que o usam, sem invalidar os outros tokens da conta.
- E-mail SMTP, descrições HTTP/Swagger, README da API e mensagens dos clientes Blazor/MAUI refletem reutilização até revogação.
- Migration `20260923131940_TokensReutilizaveisAteRevogacao` e snapshot incluem a tabela de tokens, ausente do histórico anterior, sem coluna de expiração.
- Script `DominoPontaDeQuina.Migrations/Sqlite/TokensReutilizaveis.sql` atualiza o esquema legado de desenvolvimento em uma transação, preservando dados e sem reativar tokens já expirados/revogados. Sua execução é explícita, uma única vez, com API parada e backup. Bases novas não precisam desse script.

## Evidências executadas

| Validação | Resultado |
| --- | --- |
| `dotnet restore DominoPontaDeQuina.slnx` | Sucesso após liberar acesso necessário ao NuGet fora do sandbox. |
| `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore --filter 'FullyQualifiedName~IdentidadeAcessoIntegrationTests\|FullyQualifiedName~ApiHttpIntegrationTests'` | 6 aprovados na validação inicial, antes da inclusão do teste de upgrade. |
| `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore` | 120 aprovados, zero falhas, zero ignorados, após todas as alterações e inclusão do upgrade SQLite. |
| `dotnet build DominoPontaDeQuina.Migrations --no-restore` | Sucesso, zero avisos/erros. |
| `dotnet ef database update --no-build --project DominoPontaDeQuina.Migrations --connection "Server=localhost;Database=DominoPontaDeQuina_T34_20260923_131940;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False"` | Banco descartável recebeu primeiro as migrations anteriores e, após compilação do arquivo gerado, `TokensReutilizaveisAteRevogacao`; resultado `Done.`. |

A autenticação integrada do SQL Server exigiu execução fora do sandbox após `Failed to generate SSPI context`. O banco descartável foi preservado; nenhum banco principal foi alterado. EF CLI 8.0.8 avisou que é anterior ao runtime 8.0.19, sem impedir geração/aplicação.

## Cenários cobertos

- Confirmação HTTP de emissão sem token no corpo; token guardado como hash na persistência.
- Mesma credencial apresentada por dois clientes HTTP simultâneos e por novo cliente, em acessos repetidos.
- Token com emissão há dez anos continua válido; novos contextos EF recuperam sua autorização sem sessão em memória.
- Nova emissão mantém o token anterior válido; revogação é individual e irreversível pelo fluxo de emissão.
- Revogação por conta sem vínculo recusada; segredo inválido ou ausente não autentica.
- Após revogação, os dois clientes e o cliente reaberto recebem 401; outro token da conta continua válido.
- Upgrade SQLite conserva registros, rejeita tokens previamente expirados/revogados e permite novos tokens sem coluna de expiração obrigatória.

## Limites e sequência

T-33 permanece pendente para contratos de autenticação direta, solicitação somente por e-mail, ativação/liberação/revinculação de jogador e webhook. A rota atual de solicitação ainda exige e-mail e senha acadêmica. T-35 a T-45 não foram marcadas como concluídas.

Entrega SMTP real, inspeção operacional de logs/telemetria, execução visual MAUI Windows e aceite externo não foram realizados. A alteração textual no MAUI não comprova execução desse cliente. Bancos SQLite existentes do usuário precisam seguir o procedimento documentado; não foram migrados automaticamente. Uma base SQL Server criada fora do histórico de migrations exige conciliação do esquema antes de aplicar a migration de criação da tabela.
