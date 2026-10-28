using System.Security.Claims;
using ECommerce.Application.Features.Authentifizierung;
using Microsoft.AspNetCore.Authentication;

namespace ECommerce.API.Pages.Konto;

internal static class WebSitzung
{
    public static Task StartenAsync(HttpContext kontext, AnmeldeAntwort anmeldung)
    {
        var ansprueche = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, anmeldung.KundeId.ToString()),
            new(ClaimTypes.Name, $"{anmeldung.Vorname} {anmeldung.Nachname}"),
            new(ClaimTypes.Email, anmeldung.EMail),
            new(ClaimTypes.Role, anmeldung.Rolle),
            new("Aktualisierungstoken", anmeldung.Aktualisierungstoken)
        };
        var identitaet = new ClaimsIdentity(ansprueche, "WebCookie");
        return kontext.SignInAsync(
            "WebCookie",
            new ClaimsPrincipal(identitaet),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });
    }
}
