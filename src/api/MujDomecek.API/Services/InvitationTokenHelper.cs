using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace MujDomecek.API.Services;

public sealed record InvitationToken(string Token, string TokenHash);

public static class InvitationTokenHelper
{
    public static InvitationToken CreateToken()
    {
        var raw = RandomNumberGenerator.GetBytes(32);
        var token = WebEncoders.Base64UrlEncode(raw);
        var tokenHash = HashToken(token);
        return new InvitationToken(token, tokenHash);
    }

    public static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToBase64String(SHA256.HashData(bytes));
    }

    public static string BuildInviteUrl(HttpRequest request, IConfiguration configuration, string token)
    {
        var baseUrl = configuration["App:FrontendBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";

        var invitePath = configuration["App:InvitePath"] ?? "/invite";
        if (!invitePath.StartsWith('/'))
            invitePath = "/" + invitePath;

        var encoded = Uri.EscapeDataString(token);
        return $"{baseUrl.TrimEnd('/')}{invitePath}?token={encoded}";
    }
}
