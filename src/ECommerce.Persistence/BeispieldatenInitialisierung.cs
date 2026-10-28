using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence;

public static class BeispieldatenInitialisierung
{
    public static async Task BeispieldatenErstellenAsync(
        AnwendungDbContext kontext,
        CancellationToken abbrechToken = default)
    {
        if (await kontext.Kategorien.AnyAsync(abbrechToken))
        {
            return;
        }

        var elektronik = new Kategorie
        {
            Name = "Elektronik",
            Beschreibung = "Praktische Technik für Alltag und Zuhause."
        };
        var wohnen = new Kategorie
        {
            Name = "Wohnen",
            Beschreibung = "Schöne und nützliche Dinge für Ihr Zuhause."
        };
        var accessoires = new Kategorie
        {
            Name = "Accessoires",
            Beschreibung = "Kleine Begleiter für jeden Tag."
        };

        kontext.Kategorien.AddRange(elektronik, wohnen, accessoires);
        kontext.Produkte.AddRange(
            ProduktErstellen("Kabellose Kopfhörer", "Klarer Klang und angenehmer Sitz für unterwegs.", 79.90m, 24, elektronik),
            ProduktErstellen("Schreibtischleuchte", "Dimmbare LED-Leuchte mit warmweißem Licht.", 39.50m, 18, wohnen),
            ProduktErstellen("Thermobecher", "Doppelwandiger Edelstahlbecher für heiße und kalte Getränke.", 24.90m, 42, accessoires),
            ProduktErstellen("Bluetooth-Lautsprecher", "Kompakter Lautsprecher mit kräftigem Klang.", 59.00m, 15, elektronik),
            ProduktErstellen("Woll-Decke", "Weiche Decke aus einer gemütlichen Wollmischung.", 89.00m, 12, wohnen),
            ProduktErstellen("Ledergeldbörse", "Schlanke Geldbörse mit mehreren Kartenfächern.", 34.90m, 30, accessoires),
            ProduktErstellen("Mechanische Tastatur", "Kompakte Tastatur für konzentriertes Arbeiten.", 99.00m, 10, elektronik),
            ProduktErstellen("Keramikvase", "Handgefertigte Vase mit zeitloser Form.", 29.90m, 16, wohnen));

        await kontext.SaveChangesAsync(abbrechToken);
    }

    private static Produkt ProduktErstellen(
        string name,
        string beschreibung,
        decimal preis,
        int lagerbestand,
        Kategorie kategorie) =>
        new()
        {
            Name = name,
            Beschreibung = beschreibung,
            Preis = preis,
            Lagerbestand = lagerbestand,
            Kategorie = kategorie
        };
}
