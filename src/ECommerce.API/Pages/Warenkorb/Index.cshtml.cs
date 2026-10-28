using System.Security.Claims;
using ECommerce.Application.Features.Warenkorb;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.API.Pages.Warenkorb;

public sealed class IndexModel : PageModel
{
    private readonly ISender _sender;

    public IndexModel(ISender sender) => _sender = sender;

    public WarenkorbDto Warenkorb { get; private set; } = null!;

    public async Task OnGetAsync() =>
        Warenkorb = await _sender.Send(new WarenkorbAbfragen(BenutzerId()));

    public async Task<IActionResult> OnPostHinzufuegenAsync(Guid produktId)
    {
        try
        {
            await _sender.Send(new WarenkorbpositionHinzufuegen(BenutzerId(), produktId, 1));
            TempData["Erfolg"] = "Das Produkt wurde in deinen Warenkorb gelegt.";
        }
        catch (InvalidOperationException ausnahme)
        {
            TempData["Fehler"] = ausnahme.Message;
        }
        catch (KeyNotFoundException ausnahme)
        {
            TempData["Fehler"] = ausnahme.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMengeAendernAsync(Guid positionId, int menge)
    {
        if (menge is < 1 or > 100)
        {
            TempData["Fehler"] = "Die Menge muss zwischen 1 und 100 liegen.";
            return RedirectToPage();
        }

        try
        {
            await _sender.Send(new WarenkorbMengeAendern(BenutzerId(), positionId, menge));
            TempData["Erfolg"] = "Die Artikelmenge wurde aktualisiert.";
        }
        catch (InvalidOperationException ausnahme)
        {
            TempData["Fehler"] = ausnahme.Message;
        }
        catch (KeyNotFoundException ausnahme)
        {
            TempData["Fehler"] = ausnahme.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEntfernenAsync(Guid positionId)
    {
        var entfernt = await _sender.Send(new WarenkorbpositionEntfernen(BenutzerId(), positionId));
        TempData[entfernt ? "Erfolg" : "Fehler"] =
            entfernt ? "Der Artikel wurde aus deinem Warenkorb entfernt." : "Der Artikel wurde nicht gefunden.";
        return RedirectToPage();
    }

    private Guid BenutzerId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
