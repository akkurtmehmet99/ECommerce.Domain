using System.Security.Claims;
using ECommerce.Application.Features.Bestellungen;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.API.Pages.Bestellungen;

public sealed class IndexModel : PageModel
{
    private readonly ISender _sender;

    public IndexModel(ISender sender) => _sender = sender;

    public ECommerce.Application.Common.DTOs.Seitenergebnis<BestellungDto> Bestellungen { get; private set; } = null!;

    public async Task OnGetAsync(int seite = 1) =>
        Bestellungen = await _sender.Send(new MeineBestellungenAbfragen(
            BenutzerId(),
            Math.Max(1, seite),
            20));

    private Guid BenutzerId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
