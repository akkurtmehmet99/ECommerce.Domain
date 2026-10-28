using System.ComponentModel.DataAnnotations;
using ECommerce.Application.Features.Authentifizierung;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.API.Pages.Konto;

public sealed class RegistrierenModel : PageModel
{
    private readonly ISender _sender;

    public RegistrierenModel(ISender sender) => _sender = sender;

    [BindProperty]
    public RegistrierenEingabe Eingabe { get; set; } = new();

    public IActionResult OnGet() =>
        User.Identity?.IsAuthenticated == true ? RedirectToPage("/Index") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var anmeldung = await _sender.Send(new RegistrierenBefehl(
                Eingabe.Vorname,
                Eingabe.Nachname,
                Eingabe.EMail,
                Eingabe.Passwort));
            await WebSitzung.StartenAsync(HttpContext, anmeldung);
            return RedirectToPage("/Index");
        }
        catch (InvalidOperationException ausnahme)
        {
            ModelState.AddModelError(string.Empty, ausnahme.Message);
            return Page();
        }
    }
}

public sealed class RegistrierenEingabe
{
    [Required(ErrorMessage = "Bitte gib deinen Vornamen ein.")]
    [StringLength(100)]
    public string Vorname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib deinen Nachnamen ein.")]
    [StringLength(100)]
    public string Nachname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib deine E-Mail-Adresse ein.")]
    [EmailAddress(ErrorMessage = "Bitte gib eine gültige E-Mail-Adresse ein.")]
    [StringLength(254)]
    public string EMail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte wähle ein Passwort.")]
    [StringLength(256, MinimumLength = 12, ErrorMessage = "Das Passwort muss zwischen 12 und 256 Zeichen lang sein.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Das Passwort muss Groß- und Kleinbuchstaben sowie eine Zahl enthalten.")]
    public string Passwort { get; set; } = string.Empty;
}
