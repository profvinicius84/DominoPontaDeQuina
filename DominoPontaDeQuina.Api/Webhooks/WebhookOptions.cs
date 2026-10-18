namespace DominoPontaDeQuina.Api.Webhooks;

public sealed class WebhookOptions
{
    public const string Secao = "Webhook";
    public string AssinaturaSegredo { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 5;
    public int MaximoTentativas { get; set; } = 8;
    public int IntervaloVarreduraSegundos { get; set; } = 2;
    public bool PermitirLoopbackEmDesenvolvimento { get; set; }
    public int RetencaoEntreguesDias { get; set; } = 7;
    public int RetencaoFalhasDias { get; set; } = 30;
}
