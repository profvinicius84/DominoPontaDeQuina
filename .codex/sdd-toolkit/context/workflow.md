# Dominó Ponta de Quina — Fluxo SDD

## Fontes e precedência
Usar a solicitação atual do responsável, README raiz, README do Core, código/testes relevantes e contexto desta pasta. Instruções explícitas do responsável prevalecem sobre convenções herdadas. Ler AGENTS.md ou outros documentos somente quando existirem e forem aplicáveis.

Separar comportamento observado, requisito confirmado, proposta e decisão pendente. A existência de um contexto não comprova que a funcionalidade foi implementada.

## Entrega incremental
1. Definir objetivo acadêmico e componente afetado.
2. Para novas funcionalidades, registrar escopo, critérios e dúvidas em `sdd/features/<feature-id>/spec.md`, usando o template local.
3. Derivar tarefas em `sdd/features/<feature-id>/tasks.md` com responsáveis, dependências e validação.
4. Implementar incrementos pequenos com testes pertinentes.
5. Revisar comportamento, testes, aceitação e segurança conforme o risco.
6. Atualizar evidências e contexto sem declarar como concluído o que não foi verificado.

Os caminhos em sdd/features são uma convenção para futuros documentos. Não exigir roadmap, fases, waves ou ADRs inexistentes para ajustes de documentação/configuração já autorizados.

## Testes e evidência
Usar o projeto de testes existente. Avaliar testes unitários, integração, HTTP, interface e carga conforme a alteração, justificando os relevantes e os dispensados. Não criar cinco projetos de teste por obrigação.
Para funcionalidades futuras, cobrir contratos da API com clientes independentes, estados do painel e consumo MAUI quando aplicáveis. Não relaxar asserções para obter sucesso.

Comando atual para o Core:
`dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj`

Registrar comando, resultado e limitações. O continue-on-error da pipeline não dispensa verificação dos resultados.

## Git e coordenação
Respeitar a branch e as alterações locais. Não impor fluxo de promoção de outro projeto. Para entregas dos alunos, seguir o README do Core: branch no próprio repositório, sem criar fork para entrega.
Criar commits, PRs e publicar somente conforme o escopo autorizado.
Usar agentes especializados apenas quando houver solicitação ou instrução aplicável; tarefas pequenas podem ser executadas diretamente. Se houver delegação, delimitar arquivos e dependências e preservar trabalho concorrente.
