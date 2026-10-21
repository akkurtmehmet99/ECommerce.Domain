using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Persistence;

public sealed class JwtTokenDienst : ITokenDienst
{
    private readonly string _signaturschluessel;
    private readonly string _aussteller;
    private readonly string _zielgruppe;

    public JwtTokenDienst(IConfiguration konfiguration)
    {
        _signaturschluessel = konfiguration["Authentifizierung:Signaturschluessel"]
            ?? throw new InvalidOperationException(
                "Authentifizierung:Signaturschluessel muss über eine sichere Konfiguration gesetzt werden.");
        if (Encoding.UTF8.GetByteCount(_signaturschluessel) < 32)
        {
            throw new InvalidOperationException(
                "Der Authentifizierungssignaturschlüssel muss mindestens 32 UTF-8-Bytes enthalten.");
        }

        _aussteller = konfiguration["Authentifizierung:Aussteller"] ?? "ECommerce";
        _zielgruppe = konfiguration["Authentifizierung:Zielgruppe"] ?? "ECommerceKunden";
    }

    public (string Token, DateTimeOffset Ablaufzeitpunkt) Erstellen(
        Guid kundeId,
        string email,
        string rolle)
    {
        var ablaufzeitpunkt = DateTimeOffset.UtcNow.AddMinutes(20);
        var ansprueche = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, kundeId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.NameIdentifier, kundeId.ToString()),
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Role, rolle),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var schluessel = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signaturschluessel));
        var signatur = new SigningCredentials(schluessel, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _aussteller,
            _zielgruppe,
            ansprueche,
            notBefore: DateTime.UtcNow,
            expires: ablaufzeitpunkt.UtcDateTime,
            signingCredentials: signatur);

        return (new JwtSecurityTokenHandler().WriteToken(token), ablaufzeitpunkt);
    }

    public string AktualisierungstokenErstellen()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return token.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public string AktualisierungstokenHashen(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
