using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Produkte.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Produkte;

public sealed record ProduktNachIdAbfragen(Guid ProduktId) : IRequest<ProduktDto?>;
public sealed record ProduktAktualisieren(
    Guid ProduktId, string Name, string Beschreibung, decimal Preis, int Lagerbestand, Guid KategorieId)
    : IRequest<bool>;
public sealed record ProduktDeaktivieren(Guid ProduktId) : IRequest<bool>;

public sealed class ProduktAktualisierenValidator : AbstractValidator<ProduktAktualisieren>
{
    public ProduktAktualisierenValidator()
    {
        RuleFor(befehl => befehl.ProduktId).NotEmpty();
        RuleFor(befehl => befehl.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(befehl => befehl.Beschreibung).NotNull().MaximumLength(500);
        RuleFor(befehl => befehl.Preis).GreaterThan(0).PrecisionScale(18, 2, false);
        RuleFor(befehl => befehl.Lagerbestand).GreaterThanOrEqualTo(0);
        RuleFor(befehl => befehl.KategorieId).NotEmpty();
    }
}

public sealed class ProduktVerwaltungHandler :
    IRequestHandler<ProduktNachIdAbfragen, ProduktDto?>,
    IRequestHandler<ProduktAktualisieren, bool>,
    IRequestHandler<ProduktDeaktivieren, bool>
{
    private readonly IApplicationDbContext _kontext;

    public ProduktVerwaltungHandler(IApplicationDbContext kontext) => _kontext = kontext;

    public async Task<ProduktDto?> Handle(
        ProduktNachIdAbfragen abfrage,
        CancellationToken abbrechToken)
    {
        return await _kontext.Produkte.AsNoTracking()
            .Where(produkt => produkt.Id == abfrage.ProduktId &&
                              produkt.Aktiv &&
                              !produkt.Geloescht &&
                              produkt.Kategorie.Aktiv &&
                              !produkt.Kategorie.Geloescht)
            .Select(produkt => new ProduktDto(
                produkt.Id,
                produkt.Name,
                produkt.Beschreibung,
                produkt.Preis,
                produkt.Lagerbestand,
                produkt.KategorieId,
                produkt.Kategorie.Name))
            .SingleOrDefaultAsync(abbrechToken);
    }

    public async Task<bool> Handle(ProduktAktualisieren befehl, CancellationToken abbrechToken)
    {
        var produkt = await _kontext.Produkte.SingleOrDefaultAsync(
            eintrag => eintrag.Id == befehl.ProduktId && !eintrag.Geloescht,
            abbrechToken);
        if (produkt is null)
        {
            return false;
        }

        var kategorieAktiv = await _kontext.Kategorien.AnyAsync(
            kategorie => kategorie.Id == befehl.KategorieId && kategorie.Aktiv && !kategorie.Geloescht,
            abbrechToken);
        if (!kategorieAktiv)
        {
            throw new KeyNotFoundException("Die angegebene Kategorie wurde nicht gefunden.");
        }

        produkt.Name = befehl.Name.Trim();
        produkt.Beschreibung = befehl.Beschreibung.Trim();
        produkt.Preis = befehl.Preis;
        produkt.Lagerbestand = befehl.Lagerbestand;
        produkt.KategorieId = befehl.KategorieId;
        await _kontext.SaveChangesAsync(abbrechToken);
        return true;
    }

    public async Task<bool> Handle(ProduktDeaktivieren befehl, CancellationToken abbrechToken)
    {
        var produkt = await _kontext.Produkte.SingleOrDefaultAsync(
            eintrag => eintrag.Id == befehl.ProduktId && !eintrag.Geloescht,
            abbrechToken);
        if (produkt is null)
        {
            return false;
        }

        produkt.Aktiv = false;
        produkt.Geloescht = true;
        await _kontext.SaveChangesAsync(abbrechToken);
        return true;
    }
}
