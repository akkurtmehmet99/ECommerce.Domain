using ECommerce.Application.Features.Bestellungen;
using ECommerce.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.API.Pages.Verwaltung;

[Authorize(Roles = "Administrator")]
public sealed class BestellungenModel : PageModel
{
    private readonly ISender _sender;

    public BestellungenModel(ISender sender) => _sender = sender;

    public ECommerce.Application.Common.DTOs.Seitenergebnis<BestellungDto> Bestellungen { get; private set; } = null!;

    public async Task OnGetAsync(int seite = 1) =>
        Bestellungen = await _sender.Send(new AlleBestellungenAbfragen(Math.Max(1, seite), 50));

    public IReadOnlyList<Bestellstatus> Folgestatus(Bestellstatus status) => status switch
    {
        Bestellstatus.Bezahlt => [Bestellstatus.InBearbeitung],
        Bestellstatus.InBearbeitung => [Bestellstatus.Versendet],
        Bestellstatus.Versendet => [Bestellstatus.Zugestellt],
        _ => []
    };

    public async Task<IActionResult> OnPostStatusAendernAsync(Guid bestellungId, Bestellstatus status)
    {
        if (!Enum.IsDefined(status) || !Folgestatus(status).Contains(status))
        {
            TempData["Fehler"] = "Dieser Bestellstatus ist für den aktuellen Schritt nicht zulässig.";
            return RedirectToPage();
        }

        try
        {
            var aktualisiert = await _sender.Send(new BestellstatusAendern(bestellungId, status));
            TempData[aktualisiert ? "Erfolg" : "Fehler"] =
                aktualisiert ? "Der Bestellstatus wurde aktualisiert." : "Die Bestellung wurde nicht gefunden.";
        }
        catch (InvalidOperationException ausnahme)
        {
            TempData["Fehler"] = ausnahme.Message;
        }

        return RedirectToPage();
    }
}
