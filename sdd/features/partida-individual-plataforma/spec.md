# Especificação: partida-individual-plataforma — contrato de consistência

## Metadados

- Estado: decisão técnica registrada; implementação e validação pendentes nas Waves 2 e 3.
- Data: 2026-09-21.
- Fonte: `sdd/product-spec.md` e T-02 de `tasks.md`.
- Componentes afetados: Domain, Application, Infrastructure, Migrations, API e testes. Esta tarefa não altera Core, Blazor ou MAUI.

## Problema e objetivo acadêmico

Ações quase simultâneas não podem duplicar jogada, pontuação ou avanço de turno. O cliente precisa reconhecer uma rejeição e recuperar sua visão autoritativa da partida.

## Escopo incluído e excluído

Inclui versão do estado, idempotência, ordem de recebimento, conflito e cenários de validação. Exclui host, rotas, autenticação, motor, entidades, migrations e testes executáveis, que pertencem às Waves 2 e 3. Não escolhe JWT, SMTP, SignalR ou fornecedor externo.

## Estado atual e comportamento proposto

Não existe implementação de concorrência. Cada partida terá `versaoEstado` monotônica e incrementada somente por uma transição aceita. Toda ação mutável (ingressar, iniciar, jogar e passar) receberá `versaoEsperada` e `chaveIdempotencia` UUID gerada pelo cliente.

No recebimento, o servidor registrará `recebidaEmUtc` e `ordemRecebimento`, ambos gerados no servidor. Ações com mesma versão serão processadas por partida na menor tupla `(recebidaEmUtc, ordemRecebimento)`; a segunda ação observa a versão incrementada e é rejeitada. O timestamp do cliente não define prioridade. Ação aceita, nova versão e estado resultante serão gravados na mesma transação.

## Atores, segurança e invariantes

No ingresso, o cliente informa o ID da partida, o ID do jogador-perfil escolhido e o webhook; a API monta a participação após validar vínculo autenticado, ativação exclusiva, compatibilidade e vaga. Não se exige objeto ou ID prévio de participação. A idempotência do ingresso deve funcionar antes da criação dessa entidade, usando partida, jogador validado e chave da requisição.

Atualização de 2026-09-23: o autor mencionado neste contrato representa o jogador-perfil ativo e sua participação, vinculado ao usuário autenticado, e não somente o usuário. Dois jogadores-perfis da mesma credencial podem ser adversários. A ativação exclusiva, a idempotência e as notificações seguem [fluxo de clientes e webhooks](fluxo-clientes-webhooks.md). Liberação é explícita e autenticada pela chave atual; desconexão, fechamento, abandono e tempo não liberam o jogador.

Somente participante autorizado pode agir. Versão e chave não substituem validação de turno, peça, encaixe, passagem ou encerramento. A combinação partida, autor e chave identifica uma tentativa lógica: repetição idêntica devolve o resultado registrado, sem executar o motor; reutilização da mesma chave com conteúdo ou tipo diferente é inválida e não altera estado. Rejeições não incrementam versão, não pontuam e não registram jogada aceita.

`ordemRecebimento` é interna. O estado devolvido respeita a mesma filtragem de mão, participação e observação da consulta normal; não inclui tokens, e-mails ou mão adversária.

## Fluxos, contratos e erros

1. A consulta devolve a visão permitida e `versaoEstado`.
2. O cliente envia a ação com `versaoEsperada` e uma nova `chaveIdempotencia`.
3. O servidor registra a ordem, autoriza, valida e aplica transacionalmente apenas a primeira ação válida para aquela versão.
4. Sucesso devolve a versão incrementada e estado resultante permitido.
5. Repetição da mesma tentativa devolve o resultado original com `acaoIdempotente: true`.
6. Versão desatualizada ou ação posterior recebe o conflito abaixo; o cliente substitui seu estado local pelo estado retornado ou consulta novamente quando ele não puder ser incluído.

O conflito público tem código semântico estável `CONFLITO_DE_ESTADO`:

```json
{
  "codigo": "CONFLITO_DE_ESTADO",
  "versaoAtual": 42,
  "estadoAutoritativo": { "...": "visão permitida ao solicitante" },
  "acaoIdempotente": false
}
```

Rotas, códigos HTTP e OpenAPI serão definidos na Wave 3, preservando esses campos e significados.

## Concorrência e consistência

Domain guardará dados de histórico e invariantes sem duplicar regras do Core. Application executará motor e persistência uma vez. Infrastructure aplicará comparação atômica da versão e ordenação por partida. Migrations criará versão, chaves únicas e campos de ordenação. API não aceitará timestamp do cliente nem versão resultante.

## Critérios de aceitação e evidências

- AC-API-04: ação aceita incrementa a versão uma vez e a consulta seguinte reflete o estado permitido.
- AC-API-05: ação inválida ou conflito não altera estado nem versão.
- AC-API-08: duas ações com mesma versão e ordem controlada aceitam somente a primeira; a outra recebe `CONFLITO_DE_ESTADO` e estado permitido.
- AC-DOC-01 e AC-DOC-02: OpenAPI descreve versão, chave, repetição e conflito.

Ainda não há evidência executada: Waves 2 e 3 devem testar ordem de recebimento, repetição, reutilização divergente de chave e versão desatualizada em Application/Infrastructure e HTTP.

## Riscos e questões abertas

Q-01/T-01 foi resolvida pelo responsável em 2026-09-21: quando nenhuma mão inicial tiver dupla, começa quem possuir a pedra de maior valor. Para uma comparação determinística, o servidor ordena cada pedra pela face mais alta e, depois, pela outra face, ambas em ordem decrescente. A implementação desse critério pertence à Wave 1.
