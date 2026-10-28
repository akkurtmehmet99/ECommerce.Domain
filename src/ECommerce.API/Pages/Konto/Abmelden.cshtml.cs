using ECommerce.Application.Features.Authentifizierung;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.API.Pages.Konto;

public sealed class AbmeldenModel : PageModel
{
    private readonly ISender _sender;

    public AbmeldenModel(ISender sender) => _sender = sender;

    public async Task<IActionResult> OnPostAsync()
    {
        var aktualisierungstoken = User.FindFirst("Aktualisierungstoken")?.Value;
        if (!string.IsNullOrWhiteSpace(aktualisierungstoken))
        {
            await _sender.Send(new Abmelden(aktualisierungstoken));
        }

        await HttpContext.SignOutAsync("WebCookie");
        return RedirectToPage("/Index");
    }
}
