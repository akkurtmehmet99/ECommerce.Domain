using System.Security.Claims;
using ECommerce.Application.Features.Kategorien;
using ECommerce.Application.Features.Warenkorb;
using ECommerce.Application.Features.Produkte.Queries.ProduktListeAbfragen;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.API.Pages;

public sealed class IndexModel : PageModel
{
    private readonly ISender _sender;

    public IndexModel(ISender sender) => _sender = sender;

    public ECommerce.Application.Common.DTOs.Seitenergebnis<ECommerce.Application.Features.Produkte.DTOs.ProduktDto> Produkte { get; private set; } = null!;
    public IReadOnlyList<KategorieDto> Kategorien { get; private set; } = [];
    public string? Suche { get; private set; }
    public Guid? KategorieId { get; private set; }
    public int ArtIndex { get; set; }

    public async Task OnGetAsync(string? suche, Guid? kategorieId, int seite = 1)
    {
        Suche = suche?.Trim();
        KategorieId = kategorieId;
        var aktuelleSeite = Math.Max(seite, 1);
        Produkte = await _sender.Send(new ProduktListeAbfragenQuery(
            aktuelleSeite,
            12,
            Suche,
            KategorieId));
        Kategorien = await _sender.Send(new KategorienAbfragen());
    }

    public async Task<IActionResult> OnPostInWarenkorbAsync(Guid produktId)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Konto/Anmelden", new
            {
                returnUrl = Url.Page("/Produkte/Details", new { id = produktId })
            });
        }

        try
        {
            var kundeId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _sender.Send(new WarenkorbpositionHinzufuegen(kundeId, produktId, 1));
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

        return RedirectToPage("/Produkte/Details", new { id = produktId });
    }

    public string SymbolFuer(string kategorie) => kategorie switch
    {
        "Elektronik" => "◉",
        "Wohnen" => "⌂",
        "Accessoires" => "✳",
        _ => "✦"
    };
}
