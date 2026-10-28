using ECommerce.Application.Common.DTOs;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Bestellungen;

public sealed record BestellungErstellen(
    Guid KundeId,
    string Liefername,
    string Lieferstrasse,
    string LieferPostleitzahl,
    string LieferStadt,
    string LieferLand) : IRequest<BestellungDto>;

public sealed record MeineBestellungenAbfragen(Guid KundeId, int Seite = 1, int Seitengroesse = 20)
    : IRequest<Seitenergebnis<BestellungDto>>;
public sealed record AlleBestellungenAbfragen(int Seite = 1, int Seitengroesse = 20)
    : IRequest<Seitenergebnis<BestellungDto>>;
public sealed record BestellungNachIdAbfragen(Guid KundeId, Guid BestellungId) : IRequest<BestellungDto?>;
public sealed record BestellstatusAendern(Guid BestellungId, Bestellstatus Status) : IRequest<bool>;

/// <summary>Eine eingefrorene Produktposition einer Bestellung.</summary>
public sealed record BestellpositionDto(
    Guid ProduktId, string Produktname, int Menge, decimal Einzelpreis, decimal Gesamtbetrag);

/// <summary>Bestellung mit Preis- und Lieferadress-Snapshot.</summary>
public sealed record BestellungDto(
    Guid Id,
    Bestellstatus Status,
    decimal Gesamtbetrag,
    DateTimeOffset ErstelltAm,
    DateTimeOffset ReservierungLaeuftAbAm,
    string Liefername,
    string Lieferstrasse,
    string LieferPostleitzahl,
    string LieferStadt,
    string LieferLand,
    IReadOnlyList<BestellpositionDto> Positionen);

public sealed class BestellungErstellenValidator : AbstractValidator<BestellungErstellen>
{
    public BestellungErstellenValidator()
    {
        RuleFor(befehl => befehl.KundeId).NotEmpty();
        RuleFor(befehl => befehl.Liefername).NotEmpty().MaximumLength(200);
        RuleFor(befehl => befehl.Lieferstrasse).NotEmpty().MaximumLength(200);
        RuleFor(befehl => befehl.LieferPostleitzahl).NotEmpty().MaximumLength(20);
        RuleFor(befehl => befehl.LieferStadt).NotEmpty().MaximumLength(100);
        RuleFor(befehl => befehl.LieferLand).NotEmpty().Length(2);
    }
}

public sealed class BestellstatusAendernValidator : AbstractValidator<BestellstatusAendern>
{
    public BestellstatusAendernValidator()
    {
        RuleFor(befehl => befehl.BestellungId).NotEmpty();
        RuleFor(befehl => befehl.Status).IsInEnum();
        RuleFor(befehl => befehl.Status)
            .Must(status => status is Bestellstatus.InBearbeitung or Bestellstatus.Versendet or Bestellstatus.Zugestellt)
            .WithMessage("Der Status darf hier nur auf InBearbeitung, Versendet oder Zugestellt gesetzt werden.");
    }
}

public sealed class MeineBestellungenAbfragenValidator : AbstractValidator<MeineBestellungenAbfragen>
{
    public MeineBestellungenAbfragenValidator()
    {
        RuleFor(abfrage => abfrage.KundeId).NotEmpty();
        RuleFor(abfrage => abfrage.Seite).InclusiveBetween(1, 1_000_000);
        RuleFor(abfrage => abfrage.Seitengroesse).InclusiveBetween(1, 100);
    }
}

public sealed class AlleBestellungenAbfragenValidator : AbstractValidator<AlleBestellungenAbfragen>
{
    public AlleBestellungenAbfragenValidator()
    {
        RuleFor(abfrage => abfrage.Seite).InclusiveBetween(1, 1_000_000);
        RuleFor(abfrage => abfrage.Seitengroesse).InclusiveBetween(1, 100);
    }
}

public sealed class BestellungHandler :
    IRequestHandler<BestellungErstellen, BestellungDto>,
    IRequestHandler<MeineBestellungenAbfragen, Seitenergebnis<BestellungDto>>,
    IRequestHandler<AlleBestellungenAbfragen, Seitenergebnis<BestellungDto>>,
    IRequestHandler<BestellungNachIdAbfragen, BestellungDto?>,
    IRequestHandler<BestellstatusAendern, bool>
{
    private readonly IApplicationDbContext _kontext;

    public BestellungHandler(IApplicationDbContext kontext) => _kontext = kontext;

    public async Task<BestellungDto> Handle(BestellungErstellen befehl, CancellationToken abbrechToken)
    {
        await using var transaktion = await _kontext.Database.BeginTransactionAsync(abbrechToken);

        var warenkorb = await _kontext.Warenkoerbe
            .Include(eintrag => eintrag.Positionen)
            .ThenInclude(position => position.Produkt)
            .SingleOrDefaultAsync(
                eintrag => eintrag.KundeId == befehl.KundeId && !eintrag.Geloescht,
                abbrechToken);
        if (warenkorb is null || warenkorb.Positionen.Count == 0)
        {
            throw new InvalidOperationException("Der Warenkorb ist leer.");
        }

        var bestellung = new Bestellung
        {
            KundeId = befehl.KundeId,
            Liefername = befehl.Liefername.Trim(),
            ReservierungLaeuftAbAm = DateTimeOffset.UtcNow.AddMinutes(15),
            Lieferstrasse = befehl.Lieferstrasse.Trim(),
            LieferPostleitzahl = befehl.LieferPostleitzahl.Trim(),
            LieferStadt = befehl.LieferStadt.Trim(),
            LieferLand = befehl.LieferLand.Trim().ToUpperInvariant()
        };

        foreach (var warenkorbPosition in warenkorb.Positionen)
        {
            var produkt = warenkorbPosition.Produkt;
            if (produkt.Geloescht || !produkt.Aktiv)
            {
                throw new InvalidOperationException($"Das Produkt „{produkt.Name}“ ist nicht mehr verfügbar.");
            }

            var bestandGeaendert = await _kontext.Produkte
                .Where(eintrag => eintrag.Id == produkt.Id &&
                                  eintrag.Aktiv &&
                                  !eintrag.Geloescht &&
                                  eintrag.Lagerbestand >= warenkorbPosition.Menge)
                .ExecuteUpdateAsync(
                    aktualisierung => aktualisierung.SetProperty(
                        eintrag => eintrag.Lagerbestand,
                        eintrag => eintrag.Lagerbestand - warenkorbPosition.Menge),
                    abbrechToken);

            if (bestandGeaendert == 0)
            {
                throw new InvalidOperationException(
                    $"Die gewünschte Menge des Produkts „{produkt.Name}“ ist nicht mehr verfügbar.");
            }

            bestellung.Positionen.Add(new Bestellposition
            {
                ProduktId = produkt.Id,
                Produktname = produkt.Name,
                Menge = warenkorbPosition.Menge,
                Einzelpreis = produkt.Preis
            });
        }

        bestellung.Gesamtbetrag = bestellung.Positionen.Sum(position => position.Gesamtbetrag);
        bestellung.Zahlung = new Zahlung { Status = Zahlungsstatus.Ausstehend };
        _kontext.Bestellungen.Add(bestellung);
        _kontext.Warenkorbpositionen.RemoveRange(warenkorb.Positionen);
        await _kontext.SaveChangesAsync(abbrechToken);
        await transaktion.CommitAsync(abbrechToken);

        return Abbilden(bestellung);
    }

    public async Task<Seitenergebnis<BestellungDto>> Handle(
        MeineBestellungenAbfragen abfrage,
        CancellationToken abbrechToken)
    {
        var bestellungen = _kontext.Bestellungen.AsNoTracking()
            .Where(bestellung => bestellung.KundeId == abfrage.KundeId && !bestellung.Geloescht);
        return await SeiteLaden(
            bestellungen,
            abfrage.KundeId,
            abfrage.Seite,
            abfrage.Seitengroesse,
            abbrechToken);
    }

    public async Task<Seitenergebnis<BestellungDto>> Handle(
        AlleBestellungenAbfragen abfrage,
        CancellationToken abbrechToken)
    {
        var bestellungen = _kontext.Bestellungen.AsNoTracking()
            .Where(bestellung => !bestellung.Geloescht);
        return await SeiteLaden(
            bestellungen,
            null,
            abfrage.Seite,
            abfrage.Seitengroesse,
            abbrechToken);
    }

    private async Task<Seitenergebnis<BestellungDto>> SeiteLaden(
        IQueryable<Bestellung> abfrage,
        Guid? kundeId,
        int seite,
        int seitengroesse,
        CancellationToken abbrechToken)
    {
        var gesamtanzahl = await abfrage.CountAsync(abbrechToken);
        var bestellungen = await _kontext.BestellseiteLadenAsync(
            kundeId,
            (seite - 1) * seitengroesse,
            seitengroesse,
            abbrechToken);
        return new Seitenergebnis<BestellungDto>(
            bestellungen.Select(Abbilden).ToList(),
            seite,
            seitengroesse,
            gesamtanzahl,
            (int)Math.Ceiling(gesamtanzahl / (double)seitengroesse));
    }

    public async Task<BestellungDto?> Handle(
        BestellungNachIdAbfragen abfrage,
        CancellationToken abbrechToken)
    {
        var bestellung = await _kontext.Bestellungen.AsNoTracking()
            .Include(eintrag => eintrag.Positionen)
            .SingleOrDefaultAsync(
                eintrag => eintrag.Id == abfrage.BestellungId &&
                           eintrag.KundeId == abfrage.KundeId &&
                           !eintrag.Geloescht,
                abbrechToken);
        return bestellung is null ? null : Abbilden(bestellung);
    }

    public async Task<bool> Handle(BestellstatusAendern befehl, CancellationToken abbrechToken)
    {
        var bestellung = await _kontext.Bestellungen.SingleOrDefaultAsync(
            eintrag => eintrag.Id == befehl.BestellungId && !eintrag.Geloescht,
            abbrechToken);
        if (bestellung is null)
        {
            return false;
        }

        var erlaubterUebergang = (bestellung.Status, befehl.Status) switch
        {
            (Bestellstatus.Bezahlt, Bestellstatus.InBearbeitung) => true,
            (Bestellstatus.InBearbeitung, Bestellstatus.Versendet) => true,
            (Bestellstatus.Versendet, Bestellstatus.Zugestellt) => true,
            _ => false
        };
        if (!erlaubterUebergang)
        {
            throw new InvalidOperationException(
                $"Der Bestellstatus kann nicht von {bestellung.Status} auf {befehl.Status} geändert werden.");
        }

        bestellung.Status = befehl.Status;
        await _kontext.SaveChangesAsync(abbrechToken);
        return true;
    }

    private static BestellungDto Abbilden(Bestellung bestellung) =>
        new(
            bestellung.Id,
            bestellung.Status,
            bestellung.Gesamtbetrag,
            bestellung.ErstelltAm,
            bestellung.ReservierungLaeuftAbAm,
            bestellung.Liefername,
            bestellung.Lieferstrasse,
            bestellung.LieferPostleitzahl,
            bestellung.LieferStadt,
            bestellung.LieferLand,
            bestellung.Positionen.Select(position => new BestellpositionDto(
                position.ProduktId,
                position.Produktname,
                position.Menge,
                position.Einzelpreis,
                position.Gesamtbetrag)).ToList());
}
