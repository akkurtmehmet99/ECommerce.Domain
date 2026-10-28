using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence;

public sealed class AnwendungDbContext : DbContext, IApplicationDbContext
{
    public AnwendungDbContext(DbContextOptions<AnwendungDbContext> options)
        : base(options)
    {
    }

    public DbSet<Produkt> Produkte => Set<Produkt>();
    public DbSet<Kategorie> Kategorien => Set<Kategorie>();
    public DbSet<Kunde> Kunden => Set<Kunde>();
    public DbSet<Warenkorb> Warenkoerbe => Set<Warenkorb>();
    public DbSet<Warenkorbposition> Warenkorbpositionen => Set<Warenkorbposition>();
    public DbSet<Bestellung> Bestellungen => Set<Bestellung>();
    public DbSet<Bestellposition> Bestellpositionen => Set<Bestellposition>();
    public DbSet<Zahlung> Zahlungen => Set<Zahlung>();
    public DbSet<Sitzungstoken> Sitzungstoken => Set<Sitzungstoken>();

    public async Task<IReadOnlyList<Bestellung>> BestellseiteLadenAsync(
        Guid? kundeId,
        int ueberspringen,
        int anzahl,
        CancellationToken cancellationToken)
    {
        var idsAbfrage = kundeId.HasValue
            ? Database.SqlQuery<Guid>(
                $"SELECT \"Id\" AS \"Value\" FROM \"Bestellungen\" WHERE \"Geloescht\" = 0 AND \"KundeId\" = {kundeId.Value} ORDER BY \"ErstelltAm\" DESC LIMIT {anzahl} OFFSET {ueberspringen}")
            : Database.SqlQuery<Guid>(
                $"SELECT \"Id\" AS \"Value\" FROM \"Bestellungen\" WHERE \"Geloescht\" = 0 ORDER BY \"ErstelltAm\" DESC LIMIT {anzahl} OFFSET {ueberspringen}");

        // SQLite speichert die UTC-Zeitstempel als Text und EF Core kann DateTimeOffset nicht sortieren.
        var ids = await idsAbfrage.ToListAsync(cancellationToken);
        if (ids.Count == 0)
        {
            return [];
        }

        var eintraege = await Bestellungen.AsNoTracking()
            .Where(bestellung => ids.Contains(bestellung.Id))
            .Include(bestellung => bestellung.Positionen)
            .ToListAsync(cancellationToken);
        var nachId = eintraege.ToDictionary(bestellung => bestellung.Id);
        return ids.Select(id => nachId[id]).ToList();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produkt>(entity =>
        {
            entity.ToTable("Produkte", tabelle =>
                tabelle.HasCheckConstraint("CK_Produkte_Lagerbestand_NichtNegativ", "\"Lagerbestand\" >= 0"));
            entity.Property(produkt => produkt.Name).HasMaxLength(100).IsRequired();
            entity.Property(produkt => produkt.Beschreibung).HasMaxLength(500);
            entity.Property(produkt => produkt.Preis).HasPrecision(18, 2);
            entity.HasOne(produkt => produkt.Kategorie)
                .WithMany(kategorie => kategorie.Produkte)
                .HasForeignKey(produkt => produkt.KategorieId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(produkt => produkt.KategorieId);
            entity.HasIndex(produkt => new
            {
                produkt.Aktiv,
                produkt.Geloescht,
                produkt.KategorieId,
                produkt.Name
            });
        });

        modelBuilder.Entity<Kategorie>(entity =>
        {
            entity.ToTable("Kategorien");
            entity.Property(kategorie => kategorie.Name).HasMaxLength(100).IsRequired();
            entity.Property(kategorie => kategorie.Beschreibung).HasMaxLength(500);
            entity.HasIndex(kategorie => kategorie.Name).IsUnique();
            entity.HasIndex(kategorie => new { kategorie.Aktiv, kategorie.Geloescht, kategorie.Name });
        });

        modelBuilder.Entity<Kunde>(entity =>
        {
            entity.ToTable("Kunden");
            entity.Property(kunde => kunde.Vorname).HasMaxLength(100).IsRequired();
            entity.Property(kunde => kunde.Nachname).HasMaxLength(100).IsRequired();
            entity.Property(kunde => kunde.EMail).HasMaxLength(254).IsRequired();
            entity.Property(kunde => kunde.PasswortHash).HasMaxLength(512).IsRequired();
            entity.Property(kunde => kunde.Rolle).HasMaxLength(32).IsRequired();
            entity.HasIndex(kunde => kunde.EMail).IsUnique();
        });

        modelBuilder.Entity<Warenkorb>(entity =>
        {
            entity.ToTable("Warenkoerbe");
            entity.HasIndex(warenkorb => warenkorb.KundeId).IsUnique();
            entity.HasOne(warenkorb => warenkorb.Kunde)
                .WithOne(kunde => kunde.Warenkorb)
                .HasForeignKey<Warenkorb>(warenkorb => warenkorb.KundeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Warenkorbposition>(entity =>
        {
            entity.ToTable("Warenkorbpositionen", tabelle =>
                tabelle.HasCheckConstraint("CK_Warenkorbpositionen_Menge_Gueltig", "\"Menge\" BETWEEN 1 AND 100"));
            entity.HasIndex(position => new { position.WarenkorbId, position.ProduktId }).IsUnique();
            entity.HasOne(position => position.Warenkorb)
                .WithMany(warenkorb => warenkorb.Positionen)
                .HasForeignKey(position => position.WarenkorbId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(position => position.Produkt)
                .WithMany()
                .HasForeignKey(position => position.ProduktId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(position => position.Menge).IsRequired();
        });

        modelBuilder.Entity<Bestellung>(entity =>
        {
            entity.ToTable("Bestellungen");
            entity.Property(bestellung => bestellung.Gesamtbetrag).HasPrecision(18, 2);
            entity.HasIndex(bestellung => new { bestellung.Status, bestellung.ReservierungLaeuftAbAm });
            entity.Property(bestellung => bestellung.Liefername).HasMaxLength(200).IsRequired();
            entity.Property(bestellung => bestellung.Lieferstrasse).HasMaxLength(200).IsRequired();
            entity.Property(bestellung => bestellung.LieferPostleitzahl).HasMaxLength(20).IsRequired();
            entity.Property(bestellung => bestellung.LieferStadt).HasMaxLength(100).IsRequired();
            entity.Property(bestellung => bestellung.LieferLand).HasMaxLength(2).IsRequired();
            entity.HasIndex(bestellung => new { bestellung.KundeId, bestellung.ErstelltAm });
            entity.HasOne(bestellung => bestellung.Kunde)
                .WithMany(kunde => kunde.Bestellungen)
                .HasForeignKey(bestellung => bestellung.KundeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Bestellposition>(entity =>
        {
            entity.ToTable("Bestellpositionen", tabelle =>
                tabelle.HasCheckConstraint("CK_Bestellpositionen_Menge_Positiv", "\"Menge\" > 0"));
            entity.Property(position => position.Produktname).HasMaxLength(100).IsRequired();
            entity.Property(position => position.Einzelpreis).HasPrecision(18, 2);
            entity.HasOne(position => position.Bestellung)
                .WithMany(bestellung => bestellung.Positionen)
                .HasForeignKey(position => position.BestellungId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(position => position.Produkt)
                .WithMany(produkt => produkt.Bestellpositionen)
                .HasForeignKey(position => position.ProduktId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Zahlung>(entity =>
        {
            entity.ToTable("Zahlungen");
            entity.Property(zahlung => zahlung.Transaktionsreferenz).HasMaxLength(100);
            entity.HasIndex(zahlung => zahlung.BestellungId).IsUnique();
            entity.HasOne(zahlung => zahlung.Bestellung)
                .WithOne(bestellung => bestellung.Zahlung)
                .HasForeignKey<Zahlung>(zahlung => zahlung.BestellungId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sitzungstoken>(entity =>
        {
            entity.ToTable("Sitzungstoken");
            entity.Property(token => token.Hash).HasMaxLength(64).IsRequired();
            entity.Property(token => token.ErsetztDurchHash).HasMaxLength(64);
            entity.HasIndex(token => token.Hash).IsUnique();
            entity.HasIndex(token => new { token.KundeId, token.Ablaufzeitpunkt });
            entity.HasOne(token => token.Kunde)
                .WithMany(kunde => kunde.Sitzungstoken)
                .HasForeignKey(token => token.KundeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var jetzt = DateTimeOffset.UtcNow;

        foreach (var eintrag in ChangeTracker.Entries<BaseEntity>())
        {
            if (eintrag.State == EntityState.Added)
            {
                eintrag.Entity.ErstelltAm = jetzt;
            }
            else if (eintrag.State == EntityState.Modified)
            {
                eintrag.Entity.GeaendertAm = jetzt;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
