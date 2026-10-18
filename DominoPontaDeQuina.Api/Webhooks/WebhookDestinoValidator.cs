using System.Net;
using System.Net.Sockets;

namespace DominoPontaDeQuina.Api.Webhooks;

public sealed class WebhookDestinoValidator(IHostEnvironment ambiente, Microsoft.Extensions.Options.IOptions<WebhookOptions> options)
{
    public async Task<bool> EhPermitidoAsync(Uri destino, CancellationToken ct)
    {
        if (destino.Scheme != Uri.UriSchemeHttps || destino.UserInfo.Length != 0 || destino.Fragment.Length != 0)
            return false;
        return (await ResolverPermitidosAsync(destino.DnsSafeHost, ct)).Length > 0;
    }

    public async Task<IPAddress[]> ResolverPermitidosAsync(string host, CancellationToken ct)
    {
        IPAddress[] enderecos;
        if (IPAddress.TryParse(host, out var literal))
            return EhPublicoOuLoopbackDeDesenvolvimento(literal) ? [literal] : [];
        try { enderecos = await Dns.GetHostAddressesAsync(host, ct); }
        catch (SocketException) { return []; }
        catch (ArgumentException) { return []; }
        return enderecos.Length > 0 && enderecos.All(EhPublicoOuLoopbackDeDesenvolvimento) ? enderecos : [];
    }

    private bool EhPublicoOuLoopbackDeDesenvolvimento(IPAddress endereco)
    {
        if (endereco.IsIPv4MappedToIPv6)
            endereco = endereco.MapToIPv4();
        if (IPAddress.IsLoopback(endereco))
            return ambiente.IsDevelopment() && options.Value.PermitirLoopbackEmDesenvolvimento;
        if (endereco.Equals(IPAddress.Any) || endereco.Equals(IPAddress.IPv6Any) || endereco.Equals(IPAddress.IPv6None))
            return false;
        if (endereco.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = endereco.GetAddressBytes();
            // Somente unicast global, excluindo túneis e faixas especiais/documentação.
            return (bytes[0] & 0xe0) == 0x20 && !(bytes[0] == 0x20 && bytes[1] == 0x02)
                && !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] < 2)
                && !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8);
        }

        var b = endereco.GetAddressBytes();
        return !(b[0] is 0 or 10 or 127 || b[0] >= 224
            || b[0] == 169 && b[1] == 254
            || b[0] == 172 && b[1] is >= 16 and <= 31
            || b[0] == 192 && b[1] == 168
            || b[0] == 192 && b[1] == 0
            || b[0] == 198 && b[1] is 18 or 19 or 51
            || b[0] == 203 && b[1] == 0 && b[2] == 113
            || b[0] == 100 && b[1] is >= 64 and <= 127);
    }
}
