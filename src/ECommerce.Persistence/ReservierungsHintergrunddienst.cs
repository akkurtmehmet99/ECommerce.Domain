using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Persistence;

public sealed class ReservierungsHintergrunddienst(
    IServiceScopeFactory bereichFabrik,
    ILogger<ReservierungsHintergrunddienst> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppToken)
    {
        while (!stoppToken.IsCancellationRequested)
        {
            try
            {
                await AbgelaufeneReservierungenFreigebenAsync(stoppToken);
            }
            catch (OperationCanceledException) when (stoppToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ausnahme)
            {
                logger.LogError(ausnahme, "Abgelaufene Lagerreservierungen konnten nicht freigegeben werden.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppToken);
            }
            catch (OperationCanceledException) when (stoppToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task AbgelaufeneReservierungenFreigebenAsync(CancellationToken stoppToken)
    {
        await using var bereich = bereichFabrik.CreateAsyncScope();
        var kontext = bereich.ServiceProvider.GetRequiredService<AnwendungDbContext>();
        var reservierungsDienst = bereich.ServiceProvider.GetRequiredService<IReservierungsDienst>();
        var letzteBestellungId = Guid.Empty;
        while (!stoppToken.IsCancellationRequested)
        {
            var abfrage = kontext.Bestellungen
                .AsNoTracking()
                .Where(bestellung => bestellung.Status == Bestellstatus.Ausstehend &&
                                     bestellung.Id > letzteBestellungId);

            var bestellungen = await abfrage
                .OrderBy(bestellung => bestellung.Id)
                .Select(bestellung => bestellung.Id)
                .Take(100)
                .ToListAsync(stoppToken);
            if (bestellungen.Count == 0)
            {
                return;
            }

            foreach (var bestellungId in bestellungen)
            {
                await reservierungsDienst.AbgelaufeneBestellungFreigebenAsync(bestellungId, stoppToken);
            }

            letzteBestellungId = bestellungen[^1];
        }
    }
}
