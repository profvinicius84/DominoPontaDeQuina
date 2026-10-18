# Execução T-33 a T-39 — perfis, abertura e webhooks

Data: 2026-09-23. Incremento implementado e validado localmente até a T-39. T-40 a T-45 permanecem fora desta evidência.

## Decisões aplicadas

- Ativação permanece exclusiva e sem timeout. Liberação exige a chave atual; revinculação usa nova ativação depois da liberação.
- Um jogador com participação aguardando ou em andamento não cria nem ingressa em outra partida. Perfis distintos do mesmo usuário podem jogar entre si ou em partidas diferentes.
- Início continua explícito após o segundo ingresso.
- Solicitação somente por e-mail e autenticação direta foram especificadas; a publicação das rotas finais pertence à T-40.
- Callback usa envelope versionado privado, HMAC-SHA256, confirmação 2xx, entrega ao menos uma vez, oito tentativas, backoff limitado a quinze minutos, retenção de sete dias para entregues e trinta para abandonados.

## Implementação

- `DELETE /api/jogadores/{jogadorId}/desativar-cliente` libera somente com Bearer, vínculo da conta e `X-Jogador-Ativacao` atual.
- A Application bloqueia nova participação do mesmo jogador enquanto existir partida não encerrada.
- O Core expõe e exige `PecaAberturaObrigatoria`; tentativa diferente falha antes de retirar pedra, alterar tabuleiro ou avançar turno.
- A projeção privada informa `Aguardar`, `JogarAbertura`, `Jogar`, `Comprar`, `Passar` ou `Encerrada`. A pedra obrigatória aparece somente para o jogador inicial.
- A API gera `ParticipacaoId`. O cliente continua enviando somente partida, jogador, webhook, versão e chave de idempotência.
- Cada transição aceita adiciona `EventoWebhook` por participação na mesma confirmação EF. O payload já contém a projeção filtrada daquela versão.
- O worker reserva com lease, envia após commit, assina o corpo, confirma somente em 2xx e reagenda timeout/erro/5xx. Redirecionamentos ficam desabilitados. Falha não reverte a partida.
- O validador resolve DNS a cada tentativa e bloqueia loopback fora de desenvolvimento, faixas privadas, link-local, multicast, endereços não especificados e CGNAT. O Bearer não é copiado.

## Persistência

Migration `20260923171528_FluxoClientesWebhookConfiavel` adiciona o identificador gerado da participação, a pedra de abertura persistida e a tabela/indexes da outbox. Ela foi aplicada sobre o banco descartável anterior e toda a cadeia foi reaplicada desde uma base vazia em `DominoPontaDeQuina_T39_20260923_142500`: `InitialCreate`, estado recuperável, tokens, perfis/ativação e fluxo de webhook, todas com `Done.`. Nenhum banco de uso foi alterado.

## Validação executada

| Verificação | Resultado |
| --- | --- |
| Suíte completa `dotnet test .\DominoPontaDeQuina.Tests\DominoPontaDeQuina.Tests.csproj --no-restore` | 133 aprovados, 0 falhas, 0 ignorados. |
| Abertura obrigatória e privacidade da instrução | Cobertas no Core e na integração da jornada; pedra errada não muta estado. |
| Liberação, revinculação e participação incompatível | Cobertas por HTTP com token e prova de ativação. |
| Outbox transacional e projeções privadas | Cobertas com SQLite descartável para criação, ingresso e início. |
| Entrega 5xx seguida de 2xx | Worker reagenda após 500 e marca entregue somente após 204; assinatura presente e Authorization ausente. |
| SSRF | Destino `127.0.0.1` é recusado antes do envio em ambiente de produção. |
| SQL Server existente e criação limpa | Migration final aplicada sobre o banco T-34; cinco migrations aplicadas desde vazio no banco T-39, ambas com `Done.`. |

## Limites restantes

- T-40 deve publicar no OpenAPI o envelope/cabeçalhos do callback e as rotas finais de autenticação direta e solicitação somente por e-mail.
- T-41 e T-43 devem deduplicar `eventoId`, ordenar `versaoEstado` e manter o receptor ativo enquanto aguardam HTTP.
- A validação usou handler HTTP controlado; um receptor HTTPS externo real e telemetria operacional pertencem à matriz de T-44/T-45.
