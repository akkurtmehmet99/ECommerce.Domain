using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence;

public sealed class ReservierungsDienst(AnwendungDbContext kontext) : IReservierungsDienst
{
    public async Task<bool> AbgelaufeneBestellungFreigebenAsync(
        Guid bestellungId,
        CancellationToken abbrechToken)
    {
        await using var transaktion = await kontext.Database.BeginTransactionAsync(abbrechToken);
        var jetzt = DateTimeOffset.UtcNow;
        var bestellung = await kontext.Bestellungen
            .SingleOrDefaultAsync(eintrag => eintrag.Id == bestellungId, abbrechToken);
        if (bestellung is null ||
            bestellung.Status != Bestellstatus.Ausstehend ||
            bestellung.ReservierungLaeuftAbAm > jetzt)
        {
            await transaktion.CommitAsync(abbrechToken);
            return false;
        }

        bestellung.Status = Bestellstatus.Abgelaufen;
        bestellung.GeaendertAm = jetzt;
        var positionen = await kontext.Bestellpositionen
            .Where(position => position.BestellungId == bestellungId)
            .Select(position => new { position.ProduktId, position.Menge })
            .ToListAsync(abbrechToken);

        foreach (var position in positionen)
        {
            await kontext.Produkte
                .Where(produkt => produkt.Id == position.ProduktId)
                .ExecuteUpdateAsync(
                    aktualisierung => aktualisierung.SetProperty(
                        produkt => produkt.Lagerbestand,
                        produkt => produkt.Lagerbestand + position.Menge),
                    abbrechToken);
        }

        var zahlung = await kontext.Zahlungen
            .SingleOrDefaultAsync(eintrag => eintrag.BestellungId == bestellungId, abbrechToken);
        if (zahlung is not null && zahlung.Status == Zahlungsstatus.Ausstehend)
        {
            zahlung.Status = Zahlungsstatus.Abgelaufen;
        }

        await kontext.SaveChangesAsync(abbrechToken);
        await transaktion.CommitAsync(abbrechToken);
        return true;
    }
}
