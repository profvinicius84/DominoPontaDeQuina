# Configuração Codex e SDD

Configuração específica do projeto acadêmico Dominó Ponta de Quina.

- `config.toml`: preferências gerais de agentes.
- `agents/*.toml`: perfis com instruções, modelos e permissões.
- `sdd-toolkit/context/`: produto, regras, arquitetura, estado e fluxo de trabalho.
- `sdd-toolkit/agents/`: guias resumidos dos mesmos papéis.
- `sdd-toolkit/templates/`: modelos para futuras especificações e tarefas.

Começar pelo [produto](sdd-toolkit/context/product.md), [estado atual](sdd-toolkit/context/current-state.md) e [workflow](sdd-toolkit/context/workflow.md).
A fonte inicial é o [README raiz](../README.md), complementado pelo [README do Core](../DominoPontaDeQuina.Core/README.md) e pela solicitação do responsável.

A API para alunos, o painel Blazor e o cliente MAUI são direção de produto; não são componentes já implementados. Decisões pendentes estão em [decisions.md](sdd-toolkit/context/decisions.md).

Esta adaptação preserva modelos e sandbox dos perfis e as preferências gerais existentes. Ela não cria hosts, muda código do jogo ou executa migrations. Os arquivos de contexto são apoio aos perfis e devem ser lidos conforme suas instruções; não representam funcionalidades automaticamente implementadas.
