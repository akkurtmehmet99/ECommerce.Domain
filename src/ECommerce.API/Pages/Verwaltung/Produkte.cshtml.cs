using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Application.Features.Kategorien;
using ECommerce.Application.Features.Produkte;
using ECommerce.Application.Features.Produkte.Commands.ProduktErstellen;
using ECommerce.Application.Features.Produkte.Queries.ProduktListeAbfragen;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.API.Pages.Verwaltung;

[Authorize(Roles = "Administrator")]
public sealed class ProdukteModel : PageModel
{
    private readonly ISender _sender;

    public ProdukteModel(ISender sender) => _sender = sender;

    public IReadOnlyList<KategorieDto> Kategorien { get; private set; } = [];
    public ECommerce.Application.Common.DTOs.Seitenergebnis<ECommerce.Application.Features.Produkte.DTOs.ProduktDto> Produkte { get; private set; } = null!;

    public async Task OnGetAsync(int seite = 1) => await DatenLadenAsync(seite);

    public async Task<IActionResult> OnPostProduktErstellenAsync(
        string name,
        string? beschreibung,
        string preis,
        int lagerbestand,
        Guid kategorieId)
    {
        if (!PreisLesen(preis, out var preiswert))
        {
            ModelState.AddModelError(string.Empty, "Bitte gib einen gültigen Preis ein, zum Beispiel 29,90.");
            await DatenLadenAsync();
            return Page();
        }

        var eingabe = new ProduktEingabe
        {
            Name = name,
            Beschreibung = beschreibung ?? string.Empty,
            Preis = preiswert,
            Lagerbestand = lagerbestand,
            KategorieId = kategorieId
        };
        if (!TryValidateModel(eingabe))
        {
            await DatenLadenAsync();
            return Page();
        }
        if (eingabe.KategorieId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "Bitte wähle eine Kategorie.");
            await DatenLadenAsync();
            return Page();
        }

        try
        {
            await _sender.Send(new ProduktErstellenCommand(
                eingabe.Name,
                eingabe.Beschreibung,
                eingabe.Preis,
                eingabe.Lagerbestand,
                eingabe.KategorieId));
            TempData["Erfolg"] = "Das Produkt wurde angelegt.";
            return RedirectToPage();
        }
        catch (KeyNotFoundException ausnahme)
        {
            ModelState.AddModelError(string.Empty, ausnahme.Message);
        }

        await DatenLadenAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostProduktAktualisierenAsync(
        Guid produktId,
        string name,
        string? beschreibung,
        string preis,
        int lagerbestand,
        Guid kategorieId)
    {
        if (!PreisLesen(preis, out var preiswert))
        {
            ModelState.AddModelError(string.Empty, "Bitte gib einen gültigen Preis ein, zum Beispiel 29,90.");
            await DatenLadenAsync();
            return Page();
        }

        var eingabe = new ProduktEingabe
        {
            Name = name,
            Beschreibung = beschreibung ?? string.Empty,
            Preis = preiswert,
            Lagerbestand = lagerbestand,
            KategorieId = kategorieId
        };
        if (!TryValidateModel(eingabe))
        {
            await DatenLadenAsync();
            return Page();
        }
        if (eingabe.KategorieId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "Bitte wähle eine Kategorie.");
            await DatenLadenAsync();
            return Page();
        }

        try
        {
            var aktualisiert = await _sender.Send(new ProduktAktualisieren(
                produktId,
                eingabe.Name,
                eingabe.Beschreibung,
                eingabe.Preis,
                eingabe.Lagerbestand,
                eingabe.KategorieId));
            TempData[aktualisiert ? "Erfolg" : "Fehler"] =
                aktualisiert ? "Das Produkt wurde aktualisiert." : "Das Produkt wurde nicht gefunden.";
            return RedirectToPage();
        }
        catch (KeyNotFoundException ausnahme)
        {
            ModelState.AddModelError(string.Empty, ausnahme.Message);
        }

        await DatenLadenAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostProduktDeaktivierenAsync(Guid produktId)
    {
        var deaktiviert = await _sender.Send(new ProduktDeaktivieren(produktId));
        TempData[deaktiviert ? "Erfolg" : "Fehler"] =
            deaktiviert ? "Das Produkt wurde aus dem Sortiment genommen." : "Das Produkt wurde nicht gefunden.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostKategorieErstellenAsync(string name, string? beschreibung)
    {
        var eingabe = new KategorieEingabe { Name = name, Beschreibung = beschreibung ?? string.Empty };
        if (!TryValidateModel(eingabe))
        {
            await DatenLadenAsync();
            return Page();
        }

        try
        {
            await _sender.Send(new KategorieErstellen(eingabe.Name, eingabe.Beschreibung));
            TempData["Erfolg"] = "Die Kategorie wurde angelegt.";
            return RedirectToPage();
        }
        catch (InvalidOperationException ausnahme)
        {
            ModelState.AddModelError(string.Empty, ausnahme.Message);
        }

        await DatenLadenAsync();
        return Page();
    }

    private async Task DatenLadenAsync(int seite = 1)
    {
        Kategorien = await _sender.Send(new KategorienAbfragen());
        Produkte = await _sender.Send(new ProduktListeAbfragenQuery(Math.Max(1, seite), 100));
    }

    private static bool PreisLesen(string eingabe, out decimal preis) =>
        decimal.TryParse(
            eingabe,
            NumberStyles.AllowDecimalPoint,
            CultureInfo.GetCultureInfo("de-DE"),
            out preis);
}

public sealed class ProduktEingabe
{
    [Required(ErrorMessage = "Bitte gib einen Produktnamen ein.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Der Produktname muss zwischen 2 und 100 Zeichen lang sein.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Die Beschreibung darf höchstens 500 Zeichen lang sein.")]
    public string Beschreibung { get; set; } = string.Empty;

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99",
        ErrorMessage = "Der Preis muss größer als 0 sein.",
        ParseLimitsInInvariantCulture = true)]
    public decimal Preis { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Der Lagerbestand darf nicht negativ sein.")]
    public int Lagerbestand { get; set; }

    [Required(ErrorMessage = "Bitte wähle eine Kategorie.")]
    public Guid KategorieId { get; set; }
}

public sealed class KategorieEingabe
{
    [Required(ErrorMessage = "Bitte gib einen Kategorienamen ein.")]
    [StringLength(100, ErrorMessage = "Der Kategoriename darf höchstens 100 Zeichen lang sein.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Die Beschreibung darf höchstens 500 Zeichen lang sein.")]
    public string Beschreibung { get; set; } = string.Empty;
}
