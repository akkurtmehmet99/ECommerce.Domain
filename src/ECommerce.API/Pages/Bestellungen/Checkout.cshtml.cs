using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ECommerce.Application.Features.Authentifizierung;
using ECommerce.Application.Features.Bestellungen;
using ECommerce.Application.Features.Warenkorb;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.API.Pages.Bestellungen;

public sealed class CheckoutModel : PageModel
{
    private readonly ISender _sender;

    public CheckoutModel(ISender sender) => _sender = sender;

    [BindProperty]
    public Lieferadresse Adresse { get; set; } = new();

    public WarenkorbDto Warenkorb { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        Warenkorb = await _sender.Send(new WarenkorbAbfragen(BenutzerId()));
        if (Warenkorb.Positionen.Count == 0)
        {
            TempData["Fehler"] = "Dein Warenkorb ist leer.";
            return RedirectToPage("/Warenkorb/Index");
        }

        var profil = await _sender.Send(new EigenesProfilAbfragen(BenutzerId()));
        if (profil is not null)
        {
            Adresse.Liefername = $"{profil.Vorname} {profil.Nachname}";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Warenkorb = await _sender.Send(new WarenkorbAbfragen(BenutzerId()));
            return Page();
        }

        try
        {
            var bestellung = await _sender.Send(new BestellungErstellen(
                BenutzerId(),
                Adresse.Liefername,
                Adresse.Lieferstrasse,
                Adresse.LieferPostleitzahl,
                Adresse.LieferStadt,
                Adresse.LieferLand));
            TempData["Erfolg"] = "Deine Bestellung wurde angelegt. Schließe jetzt die Sandbox-Zahlung ab.";
            return RedirectToPage("/Bestellungen/Details", new { id = bestellung.Id });
        }
        catch (InvalidOperationException ausnahme)
        {
            ModelState.AddModelError(string.Empty, ausnahme.Message);
            Warenkorb = await _sender.Send(new WarenkorbAbfragen(BenutzerId()));
            return Page();
        }
    }

    private Guid BenutzerId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed class Lieferadresse
{
    [Required(ErrorMessage = "Bitte gib den Namen der empfangenden Person ein.")]
    [StringLength(200)]
    public string Liefername { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib Straße und Hausnummer ein.")]
    [StringLength(200)]
    public string Lieferstrasse { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib die Postleitzahl ein.")]
    [StringLength(20)]
    public string LieferPostleitzahl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib den Ort ein.")]
    [StringLength(100)]
    public string LieferStadt { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib einen zweistelligen Ländercode ein.")]
    [StringLength(2, MinimumLength = 2)]
    public string LieferLand { get; set; } = "DE";
}
