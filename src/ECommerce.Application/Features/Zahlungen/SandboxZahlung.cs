using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Zahlungen;

public sealed record SandboxZahlungAusfuehren(
    Guid KundeId, Guid BestellungId, bool Erfolgreich) : IRequest<ZahlungDto>;

public sealed record ZahlungDto(
    Guid Id,
    Guid BestellungId,
    Zahlungsstatus Status,
    Zahlungsart Art,
    string Transaktionsreferenz,
    DateTimeOffset? BezahltAm);

public sealed class SandboxZahlungValidator : AbstractValidator<SandboxZahlungAusfuehren>
{
    public SandboxZahlungValidator()
    {
        RuleFor(befehl => befehl.KundeId).NotEmpty();
        RuleFor(befehl => befehl.BestellungId).NotEmpty();
    }
}

public sealed class SandboxZahlungHandler
    : IRequestHandler<SandboxZahlungAusfuehren, ZahlungDto>
{
    private readonly IApplicationDbContext _kontext;

    public SandboxZahlungHandler(IApplicationDbContext kontext) => _kontext = kontext;

    public async Task<ZahlungDto> Handle(
        SandboxZahlungAusfuehren befehl,
        CancellationToken abbrechToken)
    {
        await using var transaktion = await _kontext.Database.BeginTransactionAsync(abbrechToken);
        var bestellung = await _kontext.Bestellungen
            .Include(eintrag => eintrag.Zahlung)
            .Include(eintrag => eintrag.Positionen)
            .SingleOrDefaultAsync(
                eintrag => eintrag.Id == befehl.BestellungId &&
                           eintrag.KundeId == befehl.KundeId &&
                           !eintrag.Geloescht,
                abbrechToken);
        if (bestellung?.Zahlung is null)
        {
            throw new KeyNotFoundException("Die Bestellung oder Zahlung wurde nicht gefunden.");
        }

        var zahlung = bestellung.Zahlung;
        if (bestellung.Status == Bestellstatus.Ausstehend &&
            bestellung.ReservierungLaeuftAbAm <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException(
                "Die Lagerreservierung ist abgelaufen. Bitte erstellen Sie eine neue Bestellung.");
        }

        if (zahlung.Status != Zahlungsstatus.Ausstehend)
        {
            return Abbilden(zahlung);
        }

        zahlung.Transaktionsreferenz = $"SANDBOX-{Guid.NewGuid():N}";
        if (befehl.Erfolgreich)
        {
            zahlung.Status = Zahlungsstatus.Erfolgreich;
            zahlung.BezahltAm = DateTimeOffset.UtcNow;
            bestellung.Status = Bestellstatus.Bezahlt;
        }
        else
        {
            zahlung.Status = Zahlungsstatus.Fehlgeschlagen;
            bestellung.Status = Bestellstatus.Storniert;

            foreach (var position in bestellung.Positionen)
            {
                await _kontext.Produkte
                    .Where(produkt => produkt.Id == position.ProduktId)
                    .ExecuteUpdateAsync(
                        aktualisierung => aktualisierung.SetProperty(
                            produkt => produkt.Lagerbestand,
                            produkt => produkt.Lagerbestand + position.Menge),
                        abbrechToken);
            }
        }

        await _kontext.SaveChangesAsync(abbrechToken);
        await transaktion.CommitAsync(abbrechToken);
        return Abbilden(zahlung);
    }

    private static ZahlungDto Abbilden(Domain.Entities.Zahlung zahlung) =>
        new(
            zahlung.Id,
            zahlung.BestellungId,
            zahlung.Status,
            zahlung.Art,
            zahlung.Transaktionsreferenz,
            zahlung.BezahltAm);
}
