using ECommerce.Domain.Enums;
using System.Globalization;

namespace ECommerce.API.Pages;

public static class Darstellung
{
    public static string StatusAnzeigen(Bestellstatus status) => status switch
    {
        Bestellstatus.Ausstehend => "Zahlung ausstehend",
        Bestellstatus.Bezahlt => "Bezahlt",
        Bestellstatus.InBearbeitung => "In Bearbeitung",
        Bestellstatus.Versendet => "Versendet",
        Bestellstatus.Zugestellt => "Zugestellt",
        Bestellstatus.Storniert => "Storniert",
        Bestellstatus.Abgelaufen => "Abgelaufen",
        _ => "Unbekannt"
    };

    public static string Euro(decimal betrag) =>
        betrag.ToString("C", CultureInfo.GetCultureInfo("de-DE"));
}
