# Plano de implementação — observações dos alunos

## Objetivo e limite de compatibilidade

Corrigir a pontuação quando uma dupla estiver em uma ponta livre, permitir que um participante encerre uma partida que não deseja continuar e tornar o contrato HTTP compreensível para clientes independentes. Preservar os campos, tipos, valores numéricos e formatos de resposta das operações existentes. A única ampliação HTTP planejada é uma rota de cancelamento que reutiliza `AcaoRequest` e `ConfirmacaoAcaoPartida`; reiniciar significa cancelar a partida atual e criar outra.

Antes de implementar, registrar na especificação de produto a regra de dupla exposta e a política de cancelamento. Em particular: qualquer um dos participantes ativos pode cancelar uma partida aguardando início ou em andamento; a partida fica `Encerrada`, sem vencedor, mantém placar e histórico já registrados e libera ambos os jogadores para uma nova partida. Uma partida encerrada naturalmente não pode ser cancelada. O cancelamento não reverte pontos nem reabre a partida.

## Etapa 1 — Corrigir a pontuação da dupla

1. Explicitar em `sdd/product-spec.md` e no README do Core que cada ponta livre de uma dupla soma as duas faces daquela pedra. Se a ponta esquerda for uma dupla, usar `valorA + valorB` da primeira peça; se a direita for dupla, usar as duas faces da última peça. Uma dupla única no tabuleiro não pode ser contada duas vezes como se fossem duas pedras distintas.
2. Ajustar `Tabuleiro.SomarPontasExternas()` e, se necessário, a integração com `Rodada.CalcularPontuacao()`, mantendo `soma / 5` apenas quando a soma positiva for múltipla de cinco.
3. Cobrir dupla na esquerda, na direita, nas duas extremidades, uma única dupla, extremos sem dupla e orientação após colagem. Incluir o caso `3 + 6/6 = 15`, que concede 3 pontos, e verificar a pontuação retornada pela API e persistida após nova consulta.
4. O ajuste vale para novas jogadas. Levantar partidas já pontuadas com a regra anterior e tratar eventual correção de dados como atividade separada, com critérios próprios; não reprocessar placares automaticamente neste incremento.

**Arquivos centrais:** `DominoPontaDeQuina.Core/Models/Tabuleiro.cs`, testes do Core e da jornada, `sdd/product-spec.md`, `DominoPontaDeQuina.Core/README.md`.

## Etapa 2 — Encerrar uma partida voluntariamente

1. Adicionar `POST /api/partidas/{partidaId}/cancelar`. Reutilizar o corpo de ação existente (`jogadorId`, `versaoEsperada`, `chaveIdempotencia`), o Bearer e `X-Jogador-Ativacao`; responder com a confirmação existente. Não adicionar campos aos estados ou ao histórico.
2. Na Application, exigir participação e ativação válidas; consultar a chave de idempotência antes de rejeitar estado encerrado; rejeitar versões antigas com `CONFLITO_DE_ESTADO`; aceitar apenas `AguardandoInicio` ou `EmAndamento`. Gravar uma ação interna de cancelamento, avançar uma versão e salvar status, horário e evento de notificação na mesma transação. Reutilizar a recuperação de conflito concorrente existente.
3. Manter `vencedorId = null` no cancelamento e não conceder pontos. Preservar a rodada e as peças para consulta. O histórico atual lista jogadas, compras e passagens; documentar que o cancelamento é observado pelo estado final e pelo evento de notificação, sem criar um `EventoHistoricoPartida` que pareça uma jogada.
4. Mostrar no MAUI a opção de cancelar em partidas aguardando início e em andamento, com confirmação clara por ser uma ação irreversível. Após o estado `Encerrada`, permitir voltar à sala e criar outra partida. Outros clientes recebem a versão final pela consulta de atualizações ou webhook.
5. Testar cancelamento antes e depois do início, autorização, repetição idempotente, duas ações concorrentes na mesma versão, tentativa após encerramento, liberação dos dois perfis para novas partidas e notificações.

**Arquivos centrais:** `IJornadaPartidaService`, comando novo na Application, `JornadaPartidaService`, `PartidasController`, enum interno `TipoAcaoPartida` (persistido como texto), MAUI, testes HTTP e de integração. Verificar o mapeamento de persistência; a inclusão de um valor textual no enum interno não deve exigir migration se o esquema atual permanecer igual.

## Etapa 3 — Fechar lacunas de documentação sem mudar respostas existentes

| Observação | Ação planejada | Critério de aceite |
| --- | --- | --- |
| `proximaAcao` | Registrar no README e na descrição do schema: HTTP `0=aguardar`, `1=jogarAbertura`, `2=jogar`, `3=comprar`, `4=passar`, `5=encerrada`; no webhook, os mesmos nomes são strings. | Cliente identifica cada valor sem consultar o código; exemplo HTTP e exemplo de webhook concordam. |
| Histórico | Documentar que jogada tem `pecaJogada` e `lado`; compra e passagem os têm como `null` no JSON atual. `passouVez=true` identifica a ação explícita de passagem; a compra que também avança o turno permanece registrada como compra com `passouVez=false`. Indicar que o turno resultante deve ser lido do estado. | Tabela por tipo de ação e exemplos reais do JSON gerado; nenhuma mudança semântica silenciosa em `passouVez`. |
| Erros `400` | Documentar os formatos atuais por rota: `RespostaErro` para rejeição de domínio, corpo vazio em algumas falhas de modelo/desserialização e `false` para e-mail inválido na emissão de token. Incluir exemplos OpenAPI de cada variante. | Cliente consegue tratar os três casos; testes de contrato verificam status, tipo de corpo e exemplos. Uniformização fica para uma versão futura, pois alteraria respostas existentes. |
| Chave de ativação | Descrever no schema e no exemplo que `chaveAtivacao` deve ser enviada, não pode ser nula e deve ter exatamente 64 caracteres hexadecimais; a validação do servidor já aplica essa regra. Preservar o formato e a aceitação atuais. | Swagger comunica a regra sem recorrer ao código. Se tornar o campo `required` ou acrescentar `pattern` ao schema for considerado mudança de contrato para geradores de clientes, manter apenas descrição e exemplos neste incremento. |
| `tabuleiro` | Documentar a ordem espacial da esquerda para a direita e a orientação de cada peça. Em tabuleiro não vazio, `tabuleiro[0].valorA` é a ponta livre esquerda e `tabuleiro[último].valorB` a direita; lista vazia não tem pontas. Distinguir os valores de encaixe da contagem de duas faces da dupla para pontuação. Incluir exemplo com várias peças orientadas. | Cliente deduz as duas pontas e escolhe `lado` sem conhecer o Core; teste confere que exemplo e projeção da API usam a mesma convenção. Não adicionar `pontaEsquerda` ou `pontaDireita` ao estado atual. |

**Arquivos centrais:** `DominoPontaDeQuina.Api/DocumentacaoSwagger.cs`, `DominoPontaDeQuina.Api/README.md`, contratos apenas para comentários XML quando necessário, testes do Swagger e exemplos HTTP. Usar exemplos gerados e verificados por testes; não rotular uma sequência inventada como resposta capturada da API.

## Ordem de entrega e verificação

1. Fechar as duas regras de produto, implementar pontuação e seus testes de regressão.
2. Implementar cancelamento de ponta a ponta e validar que uma nova partida pode ser criada com os mesmos jogadores.
3. Atualizar Swagger/README, obter amostras HTTP de uma partida de teste e verificar o documento OpenAPI gerado.
4. Executar a suíte do Core, da jornada e HTTP, seguida da suíte completa `DominoPontaDeQuina.Tests`. Validar que não houve alteração de campos, valores de enum ou formatos das operações existentes, salvo a pontuação corrigida. Registrar qualquer limitação de placares anteriores.

**Concluído quando:** `3` e `6/6` nas pontas concedem `3` pontos após a jogada; cancelar encerra a partida uma única vez, notifica os participantes e permite criar outra; os cinco pontos de documentação são reproduzíveis pelo Swagger e pelo README sem depender de leitura do código.

## Execução local

- A soma da dupla exposta foi corrigida no Core. Os testes cobrem extremidades esquerda/direita, o exemplo orientado do README e a concessão de 3 pontos na rodada.
- `POST /api/partidas/{id}/cancelar` reutiliza o corpo e a confirmação de ação. O cancelamento é registrado como tipo interno textual, encerra a rodada em andamento, emite `partida-cancelada` aos webhooks registrados e libera os perfis sem atribuir vencedor. Nenhum formato das operações existentes ou esquema de banco foi alterado.
- O cliente MAUI oferece cancelamento confirmado antes e durante a partida. Swagger e README explicam enums HTTP/webhook, histórico, variantes de `400`, chave de ativação e orientação do tabuleiro.
- `dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --no-restore`: 182 aprovados, 0 falhas, 0 ignorados. `dotnet build DominoPontaDeQuina.Maui/DominoPontaDeQuina.Maui.csproj --no-restore`: sucesso, 0 avisos, 0 erros. O aceite visual em duas janelas e qualquer implantação permanecem sem execução.
- Placares gravados antes desta correção não foram recalculados; eventual reconciliação exige identificação das partidas afetadas e uma operação de dados separada.
