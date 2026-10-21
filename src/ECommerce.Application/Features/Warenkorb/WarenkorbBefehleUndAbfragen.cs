using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Warenkorb;

public sealed record WarenkorbPositionDto(
    Guid Id, Guid ProduktId, string Produktname, decimal Einzelpreis, int Menge, decimal Gesamtbetrag);

public sealed record WarenkorbDto(
    IReadOnlyList<WarenkorbPositionDto> Positionen, int Gesamtmenge, decimal Gesamtbetrag);

public sealed record WarenkorbAbfragen(Guid KundeId) : IRequest<WarenkorbDto>;
public sealed record WarenkorbpositionHinzufuegen(Guid KundeId, Guid ProduktId, int Menge) : IRequest<WarenkorbDto>;
public sealed record WarenkorbMengeAendern(Guid KundeId, Guid PositionId, int Menge) : IRequest<WarenkorbDto>;
public sealed record WarenkorbpositionEntfernen(Guid KundeId, Guid PositionId) : IRequest<bool>;

public sealed class WarenkorbpositionHinzufuegenValidator : AbstractValidator<WarenkorbpositionHinzufuegen>
{
    public WarenkorbpositionHinzufuegenValidator()
    {
        RuleFor(befehl => befehl.KundeId).NotEmpty();
        RuleFor(befehl => befehl.ProduktId).NotEmpty();
        RuleFor(befehl => befehl.Menge).InclusiveBetween(1, 100);
    }
}

public sealed class WarenkorbMengeAendernValidator : AbstractValidator<WarenkorbMengeAendern>
{
    public WarenkorbMengeAendernValidator()
    {
        RuleFor(befehl => befehl.KundeId).NotEmpty();
        RuleFor(befehl => befehl.PositionId).NotEmpty();
        RuleFor(befehl => befehl.Menge).InclusiveBetween(1, 100);
    }
}

public sealed class WarenkorbHandler :
    IRequestHandler<WarenkorbAbfragen, WarenkorbDto>,
    IRequestHandler<WarenkorbpositionHinzufuegen, WarenkorbDto>,
    IRequestHandler<WarenkorbMengeAendern, WarenkorbDto>,
    IRequestHandler<WarenkorbpositionEntfernen, bool>
{
    private readonly IApplicationDbContext _kontext;

    public WarenkorbHandler(IApplicationDbContext kontext) => _kontext = kontext;

    public async Task<WarenkorbDto> Handle(WarenkorbAbfragen abfrage, CancellationToken abbrechToken)
    {
        var warenkorb = await WarenkorbLaden(abfrage.KundeId, abbrechToken);
        return Abbilden(warenkorb);
    }

    public async Task<WarenkorbDto> Handle(
        WarenkorbpositionHinzufuegen befehl,
        CancellationToken abbrechToken)
    {
        var produkt = await _kontext.Produkte.SingleOrDefaultAsync(
            eintrag => eintrag.Id == befehl.ProduktId && eintrag.Aktiv && !eintrag.Geloescht,
            abbrechToken);
        if (produkt is null)
        {
            throw new KeyNotFoundException("Das Produkt wurde nicht gefunden.");
        }

        if (produkt.Lagerbestand < befehl.Menge)
        {
            throw new InvalidOperationException("Die gewünschte Menge ist nicht auf Lager.");
        }

        var warenkorb = await WarenkorbLaden(befehl.KundeId, abbrechToken);
        var position = warenkorb.Positionen.SingleOrDefault(
            eintrag => eintrag.ProduktId == befehl.ProduktId);
        if (position is null)
        {
            var neuePosition = new Warenkorbposition
            {
                WarenkorbId = warenkorb.Id,
                Warenkorb = warenkorb,
                ProduktId = produkt.Id,
                Produkt = produkt,
                Menge = befehl.Menge
            };
            _kontext.Warenkorbpositionen.Add(neuePosition);
        }
        else
        {
            var neueMenge = position.Menge + befehl.Menge;
            if (neueMenge > 100 || produkt.Lagerbestand < neueMenge)
            {
                throw new InvalidOperationException("Die gewünschte Gesamtmenge ist nicht verfügbar.");
            }

            position.Menge = neueMenge;
        }

        await _kontext.SaveChangesAsync(abbrechToken);
        return Abbilden(warenkorb);
    }

    public async Task<WarenkorbDto> Handle(WarenkorbMengeAendern befehl, CancellationToken abbrechToken)
    {
        var position = await _kontext.Warenkorbpositionen
            .Include(eintrag => eintrag.Produkt)
            .Include(eintrag => eintrag.Warenkorb)
            .ThenInclude(warenkorb => warenkorb.Positionen)
            .ThenInclude(warenkorbPosition => warenkorbPosition.Produkt)
            .SingleOrDefaultAsync(
                eintrag => eintrag.Id == befehl.PositionId &&
                           eintrag.Warenkorb.KundeId == befehl.KundeId,
                abbrechToken);
        if (position is null)
        {
            throw new KeyNotFoundException("Die Warenkorbposition wurde nicht gefunden.");
        }

        if (position.Produkt.Lagerbestand < befehl.Menge)
        {
            throw new InvalidOperationException("Die gewünschte Menge ist nicht auf Lager.");
        }

        position.Menge = befehl.Menge;
        await _kontext.SaveChangesAsync(abbrechToken);
        return Abbilden(position.Warenkorb);
    }

    public async Task<bool> Handle(WarenkorbpositionEntfernen befehl, CancellationToken abbrechToken)
    {
        var position = await _kontext.Warenkorbpositionen
            .Include(eintrag => eintrag.Warenkorb)
            .SingleOrDefaultAsync(
                eintrag => eintrag.Id == befehl.PositionId &&
                           eintrag.Warenkorb.KundeId == befehl.KundeId,
                abbrechToken);
        if (position is null)
        {
            return false;
        }

        _kontext.Warenkorbpositionen.Remove(position);
        await _kontext.SaveChangesAsync(abbrechToken);
        return true;
    }

    private async Task<Domain.Entities.Warenkorb> WarenkorbLaden(Guid kundeId, CancellationToken abbrechToken)
    {
        var warenkorb = await _kontext.Warenkoerbe
            .Include(eintrag => eintrag.Positionen)
            .ThenInclude(position => position.Produkt)
            .SingleOrDefaultAsync(
                eintrag => eintrag.KundeId == kundeId && !eintrag.Geloescht,
                abbrechToken);
        if (warenkorb is not null)
        {
            return warenkorb;
        }

        var kundeVorhanden = await _kontext.Kunden.AnyAsync(kunde => kunde.Id == kundeId, abbrechToken);
        if (!kundeVorhanden)
        {
            throw new UnauthorizedAccessException("Das Benutzerkonto wurde nicht gefunden.");
        }

        warenkorb = new Domain.Entities.Warenkorb { KundeId = kundeId };
        _kontext.Warenkoerbe.Add(warenkorb);
        await _kontext.SaveChangesAsync(abbrechToken);
        return warenkorb;
    }

    private static WarenkorbDto Abbilden(Domain.Entities.Warenkorb warenkorb)
    {
        var positionen = warenkorb.Positionen
            .Where(position => !position.Produkt.Geloescht && position.Produkt.Aktiv)
            .Select(position => new WarenkorbPositionDto(
                position.Id,
                position.ProduktId,
                position.Produkt.Name,
                position.Produkt.Preis,
                position.Menge,
                position.Produkt.Preis * position.Menge))
            .ToList();

        return new WarenkorbDto(
            positionen,
            positionen.Sum(position => position.Menge),
            positionen.Sum(position => position.Gesamtbetrag));
    }
}
