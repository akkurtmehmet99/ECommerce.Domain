using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Persistence;

public static class AdministratorInitialisierung
{
    public static async Task AdministratorErstellenWennKonfiguriertAsync(
        AnwendungDbContext kontext,
        IConfiguration konfiguration,
        IPasswordHasher<Kunde> passwortHasher,
        CancellationToken abbrechToken = default)
    {
        var email = konfiguration["Administrator:EMail"]?.Trim().ToLowerInvariant();
        var passwort = konfiguration["Administrator:Passwort"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(passwort))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(passwort) ||
            passwort.Length < 16)
        {
            throw new InvalidOperationException(
                "Für den initialen Administrator müssen Administrator:EMail und ein mindestens 16 Zeichen langes Administrator:Passwort gesetzt sein.");
        }

        var administrator = await kontext.Kunden.SingleOrDefaultAsync(
            kunde => kunde.EMail == email,
            abbrechToken);
        if (administrator is null)
        {
            administrator = new Kunde
            {
                Vorname = konfiguration["Administrator:Vorname"]?.Trim() ?? "Shop",
                Nachname = konfiguration["Administrator:Nachname"]?.Trim() ?? "Administrator",
                EMail = email,
                Rolle = "Administrator"
            };
            administrator.PasswortHash = passwortHasher.HashPassword(administrator, passwort);
            kontext.Kunden.Add(administrator);
            kontext.Warenkoerbe.Add(new Warenkorb { Kunde = administrator });
        }
        else if (administrator.Rolle != "Administrator")
        {
            administrator.Rolle = "Administrator";
        }

        await kontext.SaveChangesAsync(abbrechToken);
    }
}
