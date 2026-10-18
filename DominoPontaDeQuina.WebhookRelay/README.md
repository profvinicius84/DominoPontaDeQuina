# Receptor acadêmico local

O adaptador termina HTTPS para duas instâncias do MAUI, preservando bytes do corpo e os três cabeçalhos de assinatura. Somente aceita conexões loopback e destinos fixos; não é proxy público.

Com certificado de desenvolvimento ASP.NET confiável, execute:

```powershell
dotnet run --project DominoPontaDeQuina.WebhookRelay -- --urls https://localhost:5190
```

No MAUI A use porta `5191` e webhook `https://localhost:5190/clientes/a`. No MAUI B use porta `5192` e webhook `https://localhost:5190/clientes/b`. Clique **Iniciar receptor** antes do ingresso. O segredo informado no MAUI deve ser o `Webhook__AssinaturaSegredo` da API, transferido pelo responsável. Ambos validam a assinatura antes de aceitar o estado.

A API deve usar ambiente Development com `Webhook__PermitirLoopbackEmDesenvolvimento=true`. Certificados continuam sendo validados; não desabilite validação TLS. Em máquinas distintas, configure um receptor HTTPS alcançável; este relay atende somente o exercício local.
