# Cliente HTTP e atualização de estados

Biblioteca independente de Core, Application e banco. `DominoApiClient` expõe solicitação de token por e-mail, perfis, ativação, liberação, listagem, ingresso, comandos e consulta de atualizações. A participação é criada pela API. Cada instância seleciona seu perfil com `SelecionarJogador(jogadorId, chaveAtivacao)`; dois clientes podem compartilhar Bearer, mas precisam de perfis e chaves de ativação distintos.

Após criar ou entrar na partida, aplique o estado devolvido pela ação e consulte periodicamente:

```csharp
var estado = await api.EstadoAsync(token, partidaId);
var novo = await api.AtualizacoesAsync(token, partidaId, estado.VersaoEstado);
if (novo is not null) AtualizarInterface(novo);
```

`AtualizacoesAsync` envia Bearer, `jogadorId` e `X-Jogador-Ativacao`. Retorna `null` para `204 No Content`, ou o estado privado atualizado para `200 OK`. Consulte enquanto a partida estiver aberta e aplique somente versões mais novas. A resposta de uma ação já contém o estado confirmado; `FluxoEstadoCliente` com `EsperaCallback = TimeSpan.Zero` pode aplicá-lo imediatamente e proteger contra versões antigas.

Consulte o estado antes de alterar uma intenção de resultado incerto. Repetir uma ação conserva o UUID e os dados originais; criação não tem idempotência automática. Conflitos oferecem estado autoritativo. Falhas de rede não devem ser apresentadas como sucesso.

`ReceptorWebhook` e o suporte a callback em `FluxoEstadoCliente` permanecem para clientes anteriores. O cliente MAUI atual usa somente consultas e não precisa de receptor, relay, segredo ou URL pública.
