# Dominó Ponta de Quina — Arquitetura

## Estrutura observada
- `DominoPontaDeQuina.Core`: modelos, interfaces, enums e regras do jogo.
- `DominoPontaDeQuina.Domain`: entidades persistentes e contratos de repositório; sem referências a outros projetos.
- `DominoPontaDeQuina.Application`: serviços de aplicação; depende de Domain.
- `DominoPontaDeQuina.Infrastructure`: EF Core, mapeamentos e repositórios; depende de Application e Domain.
- `DominoPontaDeQuina.Migrations`: migrations em projeto separado; depende de Infrastructure.
- `DominoPontaDeQuina.Tests`: testes do Core.
- `DominoPontaDeQuina.Repository`: implementação adicional com SQLite, fora da solução atual. Não confundir seu contexto com o contexto de Infrastructure.

Core e Domain têm modelos homônimos. A integração das regras do Core com os serviços de Application precisa ser especificada; não considerar o registro de um Lance como execução completa de uma jogada.

## Direção para a evolução
A API deve expor todos os casos de uso necessários para uma partida completa por contratos documentados e consumíveis pelos alunos, pelo painel Blazor e pelo exemplo MAUI. Regras e validação autoritativa das jogadas ficam no servidor, fora das interfaces de apresentação. Clientes não acessam o banco diretamente.

O acesso será solicitado por operação pública específica com e-mail previamente cadastrado e entregue somente por e-mail autorizado. O formato do token e o mecanismo de entrega são escolhas técnicas; a resposta pública deve permanecer genérica, sem token ou enumeração de e-mails. A API é publicada à turma somente depois de completa, documentada e validada.

Modo de hospedagem Blazor e transporte de atualização de partidas ainda precisam de definição técnica. A autenticação usa solicitação pública de token para e-mail pré-cadastrado, com entrega por e-mail e resposta uniforme; o formato do token e o fornecedor de e-mail permanecem escolhas técnicas. A concorrência usa prioridade por timestamp de recebimento no servidor, cabendo definir o formato de conflito e a versão do estado. O MAUI terá suporte a Windows. Não impor BFF, JWT, SMTP, SignalR ou uma nova arquitetura de serviços como decisões já tomadas.
