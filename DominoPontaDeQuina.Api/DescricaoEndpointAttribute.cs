namespace DominoPontaDeQuina.Api;

public sealed record CriarJogadorRequest(string? Nome);
public sealed record AtivarJogadorRequest(string? ChaveAtivacao);

[AttributeUsage(AttributeTargets.Method)]
internal sealed class DescricaoEndpointAttribute(string resumo, string descricao, bool acao = false) : Attribute
{
    public string Resumo { get; } = resumo;
    public string Descricao { get; } = acao
        ? descricao + " Use versaoEsperada do último estado e um UUID novo em chaveIdempotencia para cada intenção. " +
            "Repetir a mesma intenção com a mesma chave não duplica a ação. Em 409, atualize a cópia local com estadoAutoritativo."
        : descricao;
}
