using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Kategorien;

public sealed record KategorieDto(Guid Id, string Name, string Beschreibung);
public sealed record KategorienAbfragen : IRequest<IReadOnlyList<KategorieDto>>;
public sealed record KategorieErstellen(string Name, string Beschreibung) : IRequest<Guid>;
public sealed record KategorieAktualisieren(Guid Id, string Name, string Beschreibung) : IRequest<bool>;
public sealed record KategorieLoeschen(Guid Id) : IRequest<bool>;

public sealed class KategorieErstellenValidator : AbstractValidator<KategorieErstellen>
{
    public KategorieErstellenValidator()
    {
        RuleFor(befehl => befehl.Name).NotEmpty().MaximumLength(100);
        RuleFor(befehl => befehl.Beschreibung).MaximumLength(500);
    }
}

public sealed class KategorieAktualisierenValidator : AbstractValidator<KategorieAktualisieren>
{
    public KategorieAktualisierenValidator()
    {
        RuleFor(befehl => befehl.Id).NotEmpty();
        RuleFor(befehl => befehl.Name).NotEmpty().MaximumLength(100);
        RuleFor(befehl => befehl.Beschreibung).MaximumLength(500);
    }
}

public sealed class KategorienHandler :
    IRequestHandler<KategorienAbfragen, IReadOnlyList<KategorieDto>>,
    IRequestHandler<KategorieErstellen, Guid>,
    IRequestHandler<KategorieAktualisieren, bool>,
    IRequestHandler<KategorieLoeschen, bool>
{
    private readonly IApplicationDbContext _kontext;

    public KategorienHandler(IApplicationDbContext kontext) => _kontext = kontext;

    public async Task<IReadOnlyList<KategorieDto>> Handle(
        KategorienAbfragen abfrage,
        CancellationToken abbrechToken) =>
        await _kontext.Kategorien.AsNoTracking()
            .Where(kategorie => kategorie.Aktiv && !kategorie.Geloescht)
            .OrderBy(kategorie => kategorie.Name)
            .Select(kategorie => new KategorieDto(
                kategorie.Id, kategorie.Name, kategorie.Beschreibung))
            .ToListAsync(abbrechToken);

    public async Task<Guid> Handle(KategorieErstellen befehl, CancellationToken abbrechToken)
    {
        var name = befehl.Name.Trim();
        if (await _kontext.Kategorien.AnyAsync(
                kategorie => kategorie.Name.ToLower() == name.ToLower() && !kategorie.Geloescht,
                abbrechToken))
        {
            throw new InvalidOperationException("Eine Kategorie mit diesem Namen existiert bereits.");
        }

        var kategorie = new Kategorie { Name = name, Beschreibung = befehl.Beschreibung.Trim() };
        _kontext.Kategorien.Add(kategorie);
        await _kontext.SaveChangesAsync(abbrechToken);
        return kategorie.Id;
    }

    public async Task<bool> Handle(KategorieAktualisieren befehl, CancellationToken abbrechToken)
    {
        var kategorie = await _kontext.Kategorien.SingleOrDefaultAsync(
            eintrag => eintrag.Id == befehl.Id && !eintrag.Geloescht,
            abbrechToken);
        if (kategorie is null)
        {
            return false;
        }

        kategorie.Name = befehl.Name.Trim();
        kategorie.Beschreibung = befehl.Beschreibung.Trim();
        await _kontext.SaveChangesAsync(abbrechToken);
        return true;
    }

    public async Task<bool> Handle(KategorieLoeschen befehl, CancellationToken abbrechToken)
    {
        var kategorie = await _kontext.Kategorien.SingleOrDefaultAsync(
            eintrag => eintrag.Id == befehl.Id && !eintrag.Geloescht,
            abbrechToken);
        if (kategorie is null)
        {
            return false;
        }

        var besitztProdukte = await _kontext.Produkte.AnyAsync(
            produkt => produkt.KategorieId == befehl.Id && !produkt.Geloescht,
            abbrechToken);
        if (besitztProdukte)
        {
            throw new InvalidOperationException(
                "Kategorien mit Produkten können nicht gelöscht werden. Deaktivieren Sie die Produkte zuerst.");
        }

        kategorie.Geloescht = true;
        kategorie.Aktiv = false;
        await _kontext.SaveChangesAsync(abbrechToken);
        return true;
    }
}
