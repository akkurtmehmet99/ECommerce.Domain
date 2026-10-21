using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ECommerce.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Produkt> Produkte { get; }
    DbSet<Kategorie> Kategorien { get; }
    DbSet<Kunde> Kunden { get; }
    DbSet<Warenkorb> Warenkoerbe { get; }
    DbSet<Warenkorbposition> Warenkorbpositionen { get; }
    DbSet<Bestellung> Bestellungen { get; }
    DbSet<Bestellposition> Bestellpositionen { get; }
    DbSet<Zahlung> Zahlungen { get; }
    DbSet<Sitzungstoken> Sitzungstoken { get; }
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
