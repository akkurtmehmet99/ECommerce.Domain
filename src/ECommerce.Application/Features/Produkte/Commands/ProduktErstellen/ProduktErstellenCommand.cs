using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Produkte.Commands.ProduktErstellen;

public sealed record ProduktErstellenCommand(
    string Name,
    string Beschreibung,
    decimal Preis,
    int Lagerbestand,
    Guid KategorieId) : IRequest<Guid>;

public sealed class ProduktErstellenCommandHandler : IRequestHandler<ProduktErstellenCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public ProduktErstellenCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(ProduktErstellenCommand request, CancellationToken cancellationToken)
    {
        var kategorieVorhanden = await _context.Kategorien
            .AnyAsync(kategorie => kategorie.Id == request.KategorieId && kategorie.Aktiv,
                cancellationToken);
        if (!kategorieVorhanden)
        {
            throw new KeyNotFoundException("Die angegebene Kategorie wurde nicht gefunden.");
        }

        var produkt = new Produkt
        {
            Name = request.Name.Trim(),
            Beschreibung = request.Beschreibung.Trim(),
            Preis = request.Preis,
            Lagerbestand = request.Lagerbestand,
            KategorieId = request.KategorieId
        };

        _context.Produkte.Add(produkt);
        await _context.SaveChangesAsync(cancellationToken);

        return produkt.Id;
    }
}
