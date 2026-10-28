using ECommerce.Application.Features.Produkte;
using ECommerce.Application.Features.Produkte.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.API.Pages.Produkte;

public sealed class DetailsModel : PageModel
{
    private readonly ISender _sender;

    public DetailsModel(ISender sender) => _sender = sender;

    public ProduktDto Produkt { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var produkt = await _sender.Send(new ProduktNachIdAbfragen(id));
        if (produkt is null)
        {
            return NotFound();
        }

        Produkt = produkt;
        return Page();
    }
}
