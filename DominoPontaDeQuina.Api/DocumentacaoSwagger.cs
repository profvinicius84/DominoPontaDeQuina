using DominoPontaDeQuina.Application.Partidas.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DominoPontaDeQuina.Api;

internal static class DocumentacaoSwagger
{
    public static IServiceCollection AddDocumentacaoSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Dominó Ponta de Quina — API",
                Version = "v1",
                Description = "Partidas individuais entre dois jogadores. Solicite um token por e-mail e cole somente o token em Authorize. " +
                    "Depois crie uma partida, ingresse com o segundo jogador e inicie. Consulte o estado antes de agir. " +
                    "JSON usa camelCase e enums numéricos. Em desenvolvimento, a entrega de e-mail depende da configuração SendGrid."
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                Description = "Token opaco recebido por e-mail (não é JWT). Informe somente o token, sem o prefixo Bearer."
            });
            options.OperationFilter<ContratoOperationFilter>();
            options.SchemaFilter<ContratoSchemaFilter>();
        });
        return services;
    }

}

internal sealed class ContratoOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var action = context.MethodInfo;
        var controller = action.DeclaringType;
        var descricao = action.GetCustomAttributes(typeof(DescricaoEndpointAttribute), true)
            .OfType<DescricaoEndpointAttribute>().FirstOrDefault();
        if (descricao is not null)
        {
            operation.Summary = descricao.Resumo;
            operation.Description = descricao.Descricao;
        }

        var exigeAutorizacao = metadata.OfType<IAuthorizeData>().Any()
            || action.IsDefined(typeof(AuthorizeAttribute), true)
            || controller?.IsDefined(typeof(AuthorizeAttribute), true) == true;
        var permiteAnonimo = metadata.OfType<IAllowAnonymous>().Any()
            || action.IsDefined(typeof(AllowAnonymousAttribute), true);
        if (exigeAutorizacao && !permiteAnonimo)
            operation.Security = [new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
            }];

        var rota = (context.ApiDescription.RelativePath ?? string.Empty).Trim('/');
        if ((rota.StartsWith("api/partidas", StringComparison.Ordinal) && !rota.EndsWith("visualizar", StringComparison.Ordinal)
            && !(rota.TrimEnd('/') == "api/partidas" && context.ApiDescription.HttpMethod == "GET"))
            || (rota.StartsWith("api/jogadores") && context.ApiDescription.HttpMethod == "DELETE"))
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Jogador-Ativacao", In = ParameterLocation.Header, Required = true,
                Description = "Chave secreta da ativação do jogador selecionado, preservada pelo cliente. Não é o token da conta.",
                Schema = new OpenApiSchema { Type = "string", Format = "password" }
            });

        if (context.ApiDescription.HttpMethod == "POST" &&
            (rota == "api/partidas" || rota.EndsWith("/entrar")))
        {
            var estado = context.SchemaGenerator.GenerateSchema(typeof(EstadoPartidaParticipante), context.SchemaRepository);
            var privado = context.SchemaRepository.Schemas[estado.Reference.Id];
            var propriedades = privado.Properties.ToDictionary(p => p.Key, p => p.Value);
            propriedades["situacao"] = EnumTexto("aguardandoInicio", "emAndamento", "encerrada");
            propriedades["proximaAcao"] = EnumTexto("aguardar", "jogarAbertura", "jogar", "comprar", "passar", "encerrada");
            var envelope = new OpenApiSchema { Type = "object", Properties = new Dictionary<string, OpenApiSchema>
            {
                ["eventoId"] = new() { Type = "string", Format = "uuid" },
                ["partidaId"] = new() { Type = "string", Format = "uuid" },
                ["participacaoId"] = new() { Type = "string", Format = "uuid" },
                ["versaoEstado"] = new() { Type = "integer", Format = "int64" },
                ["tipo"] = new() { Type = "string" },
                ["ocorridoEmUtc"] = new() { Type = "string", Format = "date-time" },
                ["estado"] = new() { Type = "object", Properties = propriedades }
            }};
            var callback = new OpenApiCallback();
            callback.AddPathItem(Microsoft.OpenApi.Expressions.RuntimeExpression.Build("{$request.body#/webhook}"),
                new OpenApiPathItem { Operations = new Dictionary<OperationType, OpenApiOperation>
                { [OperationType.Post] = new()
                {
                    Summary = "Estado privado da participação",
                    Description = "Compatibilidade para clientes que informam webhook. O fluxo recomendado consulta /atualizacoes. Após commit, entrega ao menos uma vez; deduplicar eventoId e ordenar versaoEstado. HMAC-SHA256 do instante + ponto + bytes UTF-8 exatos do corpo; validar assinatura e janela de 5 minutos. Nunca recebe Bearer.",
                    Parameters = new[] { "X-Domino-Evento", "X-Domino-Instante", "X-Domino-Assinatura" }
                        .Select(n => new OpenApiParameter { Name = n, In = ParameterLocation.Header, Required = true, Schema = new() { Type = "string" } }).ToList(),
                    RequestBody = new() { Required = true, Content = new Dictionary<string, OpenApiMediaType>
                        { ["application/json"] = new() { Schema = envelope } } },
                    Responses = new() { ["204"] = new() { Description = "Recebido ou duplicado. Qualquer 2xx confirma." },
                        ["503"] = new() { Description = "Falha temporária; a API repetirá." } }
                } } });
            operation.Callbacks["estadoAlterado"] = callback;
        }

        foreach (var parameter in operation.Parameters.Where(p => p.Name == "partidaId"))
            parameter.Description = "UUID da partida retornado na criação.";

        foreach (var (status, response) in operation.Responses)
            response.Description = status switch
            {
                "200" => "Operação concluída; retorna a visão autorizada ao solicitante.",
                "202" => "Solicitação processada, sem corpo e sem revelar cadastro. Entrega por e-mail quando elegível.",
                "204" => rota.EndsWith("/atualizacoes", StringComparison.Ordinal)
                    ? "Sem alteração desde versaoConhecida; sem corpo."
                    : "Token revogado; sem corpo.",
                "400" => rota == "api/identidade/token"
                    ? "E-mail malformado ou fora de @fiap.com.br: retorna o booleano false."
                    : "ENTRADA_INVALIDA: revise os campos. Erros de desserialização podem ser rejeitados pelo framework sem este envelope JSON.",
                "401" => "NAO_AUTENTICADO: token ausente, inválido ou revogado.",
                "403" => "SEM_PERMISSAO: usuário sem permissão para a operação.",
                "404" => "PARTIDA_INDISPONIVEL: partida não encontrada ou indisponível ao solicitante.",
                "409" => rota.StartsWith("api/jogadores", StringComparison.Ordinal)
                    ? "JOGADOR_JA_ATIVO: este perfil já está reservado por outra chave de ativação."
                    : "CONFLITO_DE_ESTADO: atualize a cópia local com estadoAutoritativo antes de uma nova intenção.",
                "422" => "Regra rejeitada: ESTADO_INVALIDO, FORA_DO_TURNO, PECA_AUSENTE, ENCAIXE_INVALIDO ou PASSAGEM_NAO_PERMITIDA. Não houve alteração.",
                "429" => "LIMITE_EXCEDIDO: máximo de cinco solicitações por e-mail e 50 por IP de origem a cada dez minutos.",
                "500" => "ERRO_INTERNO: falha inesperada ao processar a operação.",
                _ => response.Description
            };
    }

    private static OpenApiSchema EnumTexto(params string[] valores) => new()
    { Type = "string", Enum = valores.Select(v => (IOpenApiAny)new OpenApiString(v)).ToList() };
}

internal sealed class ContratoSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(LadoJogavel)) schema.Description = "Ponta do tabuleiro: 0 = esquerda; 1 = direita.";
        if (context.Type == typeof(SituacaoPartida)) schema.Description = "0 = aguardando início; 1 = em andamento; 2 = encerrada.";
        if (context.Type == typeof(ProximaAcaoPartida)) schema.Description = "HTTP: 0 = aguardar; 1 = jogarAbertura; 2 = jogar; 3 = comprar; 4 = passar; 5 = encerrada. No webhook, os nomes são enviados como strings.";
        if (context.Type == typeof(EventoHistoricoPartida)) schema.Description = "Jogada: pecaJogada e lado preenchidos, passouVez=false. Passagem explícita: pecaJogada e lado null, passouVez=true. Compra: pecaJogada e lado null, passouVez=false, mesmo quando avança o turno. Os campos nulos constam no JSON.";

        foreach (var (name, property) in schema.Properties)
        {
            property.Description = name switch
            {
                "versaoEsperada" => "Versão do último estado consultado; usada para detectar ações concorrentes.",
                "chaveIdempotencia" => "UUID novo por intenção. Reutilize a mesma chave somente para repetir a mesma intenção.",
                "pontuacaoAlvo" => "Pontuação positiva necessária para vencer a partida.",
                "valorA" or "valorB" => "Valor da ponta da pedra, entre 0 e 6.",
                "senha" => "Senha acadêmica exigida para solicitar acesso.",
                "jogadorId" => "Identificador do perfil pertencente à conta autenticada; operações de jogo exigem sua ativação exclusiva.",
                "webhook" => "Opcional para compatibilidade com clientes antigos. Clientes novos consultam /atualizacoes e não precisam enviar webhook.",
                "chaveAtivacao" => "Obrigatória e não nula ao ativar: 32 bytes aleatórios em exatamente 64 caracteres hexadecimais (0-9, a-f ou A-F), gerados e guardados pelo cliente. Nunca use o token da conta como chave.",
                "email" => "E-mail FIAP que receberá o token; a conta é criada se ainda não existir.",
                "estadoAutoritativo" => "Estado atual filtrado para o participante, quando disponível; pode ser nulo.",
                "minhaMao" => "Pedras privadas do solicitante. Não inclui pedras do adversário.",
                "tabuleiro" => "Peças orientadas da esquerda para a direita. Em lista não vazia, a ponta livre esquerda é o valorA da primeira peça e a direita é o valorB da última. Lista vazia não tem pontas.",
                "proximaAcao" => "HTTP: 0=aguardar, 1=jogarAbertura, 2=jogar, 3=comprar, 4=passar, 5=encerrada. No webhook são strings.",
                "pecaJogada" => "Pedra jogada; null em compra ou passagem. O JSON atual inclui o campo nulo.",
                "lado" when context.Type == typeof(EventoHistoricoPartida) => "Lado da jogada; null em compra ou passagem. O JSON atual inclui o campo nulo.",
                "passouVez" => "True somente na ação explícita de passagem. Uma compra que também avançar o turno permanece registrada como compra com false.",
                "acaoIdempotente" => "Indica repetição de uma intenção já aceita, sem executar novamente a ação.",
                _ => property.Description
            };
            if (name is "senha" or "chaveAtivacao") { property.Format = "password"; property.WriteOnly = true; }
        }

        if (context.Type == typeof(SolicitarTokenRequest))
            schema.Example = new OpenApiObject { ["email"] = new OpenApiString("aluno@fiap.com.br") };
        if (context.Type == typeof(CriarPartidaRequest))
            schema.Example = new OpenApiObject { ["pontuacaoAlvo"] = new OpenApiInteger(50),
                ["jogadorId"] = new OpenApiString("11111111-2222-3333-4444-555555555555") };
        if (context.Type == typeof(AcaoRequest) || context.Type == typeof(JogarRequest) || context.Type == typeof(IngressarRequest))
        {
            var example = new OpenApiObject
            {
                ["jogadorId"] = new OpenApiString("11111111-2222-3333-4444-555555555555"),
                ["versaoEsperada"] = new OpenApiLong(2),
                ["chaveIdempotencia"] = new OpenApiString("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")
            };
            if (context.Type == typeof(JogarRequest))
            {
                example["valorA"] = new OpenApiInteger(6);
                example["valorB"] = new OpenApiInteger(5);
                example["lado"] = new OpenApiInteger(0);
            }
            schema.Example = example;
        }
    }
}
