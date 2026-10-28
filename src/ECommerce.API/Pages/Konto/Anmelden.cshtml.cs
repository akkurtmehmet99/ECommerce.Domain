using System.ComponentModel.DataAnnotations;
using ECommerce.Application.Features.Authentifizierung;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.API.Pages.Konto;

public sealed class AnmeldenModel : PageModel
{
    private readonly ISender _sender;

    public AnmeldenModel(ISender sender) => _sender = sender;

    [BindProperty]
    public AnmeldenEingabe Eingabe { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var anmeldung = await _sender.Send(new AnmeldenBefehl(Eingabe.EMail, Eingabe.Passwort));
            await WebSitzung.StartenAsync(HttpContext, anmeldung);
            return Url.IsLocalUrl(ReturnUrl)
                ? LocalRedirect(ReturnUrl!)
                : RedirectToPage("/Index");
        }
        catch (UnauthorizedAccessException)
        {
            ModelState.AddModelError(string.Empty, "E-Mail-Adresse oder Passwort ist ungültig.");
            return Page();
        }
    }

}

public sealed class AnmeldenEingabe
{
    [Required(ErrorMessage = "Bitte gib deine E-Mail-Adresse ein.")]
    [EmailAddress(ErrorMessage = "Bitte gib eine gültige E-Mail-Adresse ein.")]
    [StringLength(254)]
    public string EMail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib dein Passwort ein.")]
    [StringLength(256, MinimumLength = 1)]
    public string Passwort { get; set; } = string.Empty;
}
