using ECommerce.Application.Common.DTOs;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Produkte.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Produkte.Queries.ProduktListeAbfragen;

public sealed record ProduktListeAbfragenQuery(int Seite = 1, int Seitengroesse = 20)
    : IRequest<Seitenergebnis<ProduktDto>>;

public sealed class ProduktListeAbfragenValidator : AbstractValidator<ProduktListeAbfragenQuery>
{
    public ProduktListeAbfragenValidator()
    {
        RuleFor(abfrage => abfrage.Seite).InclusiveBetween(1, 1_000_000);
        RuleFor(abfrage => abfrage.Seitengroesse).InclusiveBetween(1, 100);
    }
}

public sealed class ProduktListeAbfragenQueryHandler
    : IRequestHandler<ProduktListeAbfragenQuery, Seitenergebnis<ProduktDto>>
{
    private readonly IApplicationDbContext _context;

    public ProduktListeAbfragenQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Seitenergebnis<ProduktDto>> Handle(
        ProduktListeAbfragenQuery request,
        CancellationToken cancellationToken)
    {
        var produkte = _context.Produkte
            .AsNoTracking()
            .Where(produkt => produkt.Aktiv &&
                              !produkt.Geloescht &&
                              produkt.Kategorie.Aktiv &&
                              !produkt.Kategorie.Geloescht);
        var gesamtanzahl = await produkte.CountAsync(cancellationToken);
        var eintraege = await produkte
            .OrderBy(produkt => produkt.Name)
            .Include(produkt => produkt.Kategorie)
            .Skip((request.Seite - 1) * request.Seitengroesse)
            .Take(request.Seitengroesse)
            .Select(produkt => new ProduktDto(
                produkt.Id,
                produkt.Name,
                produkt.Beschreibung,
                produkt.Preis,
                produkt.Lagerbestand,
                produkt.KategorieId,
                produkt.Kategorie.Name))
            .ToListAsync(cancellationToken);

        return new Seitenergebnis<ProduktDto>(
            eintraege,
            request.Seite,
            request.Seitengroesse,
            gesamtanzahl,
            (int)Math.Ceiling(gesamtanzahl / (double)request.Seitengroesse));
    }
}
