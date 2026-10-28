using System.Security.Claims;
using ECommerce.Application.Features.Bestellungen;
using ECommerce.Application.Features.Zahlungen;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.API.Pages.Bestellungen;

public sealed class DetailsModel : PageModel
{
    private readonly ISender _sender;

    public DetailsModel(ISender sender) => _sender = sender;

    public BestellungDto Bestellung { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var bestellung = await _sender.Send(new BestellungNachIdAbfragen(BenutzerId(), id));
        if (bestellung is null)
        {
            return NotFound();
        }

        Bestellung = bestellung;
        return Page();
    }

    public async Task<IActionResult> OnPostZahlenAsync(Guid id)
    {
        try
        {
            var zahlung = await _sender.Send(
                new SandboxZahlungAusfuehren(BenutzerId(), id, Erfolgreich: true));
            TempData["Erfolg"] = zahlung.Status == Zahlungsstatus.Erfolgreich
                ? "Die Sandbox-Zahlung war erfolgreich. Vielen Dank für deine Bestellung!"
                : "Die Zahlung konnte nicht abgeschlossen werden.";
        }
        catch (InvalidOperationException ausnahme)
        {
            TempData["Fehler"] = ausnahme.Message;
        }
        catch (KeyNotFoundException ausnahme)
        {
            TempData["Fehler"] = ausnahme.Message;
        }

        return RedirectToPage(new { id });
    }

    private Guid BenutzerId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
