using ECommerce.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ECommerce.API.Middleware;

public sealed class DatenbankGesundheitspruefung(AnwendungDbContext kontext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext kontextGesundheit,
        CancellationToken abbrechToken = default)
    {
        try
        {
            var erreichbar = await kontext.Database.CanConnectAsync(abbrechToken);
            return erreichbar
                ? HealthCheckResult.Healthy("Die Datenbank ist erreichbar.")
                : HealthCheckResult.Unhealthy("Die Datenbank ist nicht erreichbar.");
        }
        catch (Exception ausnahme)
        {
            return HealthCheckResult.Unhealthy("Die Datenbankprüfung ist fehlgeschlagen.", ausnahme);
        }
    }
}
