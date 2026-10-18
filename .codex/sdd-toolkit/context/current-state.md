# Dominó Ponta de Quina — Estado atual

Revisão em 2026-09-21, baseada no README raiz, README do Core, .slnx, .csproj, serviço PartidaService e workflow validate-tests.yml.

- A solução contém Core, Domain, Application, Infrastructure, Migrations e Tests.
- Existem modelos do jogo, serviços de aplicação, repositórios, mapeamentos e uma migration inicial.
- O projeto Tests referencia apenas Core. Não inferir cobertura da persistência ou dos serviços a partir desses testes.
- PartidaService registra partidas, jogadores e lances, e consulta status, histórico e ranking. Isso não comprova integração completa com o motor de regras.
- O README do Core registra um exercício histórico ou separado. Para a etapa atual, a equipe do projeto deve concluir e validar os gaps do motor antes de publicar a API; alunos apenas consomem os contratos documentados.
- API, painel Blazor e exemplo MAUI ainda não possuem projetos nesta solução.
- A API completa e documentada é a entrega obrigatória antes do acesso da turma. O acesso será solicitado por e-mail previamente cadastrado, com token enviado exclusivamente ao endereço autorizado; esse fluxo ainda não está implementado.
- A regra de produto vigente é uma partida entre dois jogadores, sem times, com sete pedras por mão e cava de quatorze pedras. O código atual ainda contém abstrações de Time; sua adaptação é uma tarefa futura, não uma conclusão desta revisão documental.
- Repository existe no disco e está fora da solução; sua continuidade está pendente de decisão.
- A pipeline faz restore/build/test do projeto de testes e usa `continue-on-error: true` no passo de testes. Pipeline verde não comprova testes aprovados.
- Não foram localizados roadmap, ADRs ou especificações de funcionalidades em diretórios externos a .codex nesta revisão. Caminhos para futuros artefatos são convenções propostas, não arquivos existentes.
- Nenhum build ou teste do jogo foi executado nesta revisão de configuração.

Usar caminhos relativos à raiz; não depender do diretório de uma máquina específica.
