using System.Security.Claims;
using ECommerce.API.Contracts;
using ECommerce.Application.Features.Authentifizierung;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers;

/// <summary>Registrierung, Anmeldung und Kundenprofil.</summary>
[ApiController]
[Route("api/authentifizierung")]
[Produces("application/json")]
public sealed class AuthentifizierungController : ControllerBase
{
    private readonly ISender _sender;

    public AuthentifizierungController(ISender sender) => _sender = sender;

    /// <summary>Registriert ein Kundenkonto und gibt ein Zugriffstoken zurück.</summary>
    [HttpPost("registrieren")]
    [AllowAnonymous]
    [EnableRateLimiting("authentifizierung")]
    [ProducesResponseType<AnmeldeAntwort>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnmeldeAntwort>> Registrieren(
        RegistrierenAnfrage anfrage,
        CancellationToken abbrechToken)
    {
        var antwort = await _sender.Send(
            new RegistrierenBefehl(anfrage.Vorname, anfrage.Nachname, anfrage.EMail, anfrage.Passwort),
            abbrechToken);
        return CreatedAtAction(nameof(EigenesProfil), null, antwort);
    }

    /// <summary>Meldet einen Kunden an und gibt ein Zugriffstoken zurück.</summary>
    [HttpPost("anmelden")]
    [AllowAnonymous]
    [EnableRateLimiting("authentifizierung")]
    [ProducesResponseType<AnmeldeAntwort>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AnmeldeAntwort>> Anmelden(
        AnmeldenAnfrage anfrage,
        CancellationToken abbrechToken)
    {
        var antwort = await _sender.Send(
            new AnmeldenBefehl(anfrage.EMail, anfrage.Passwort),
            abbrechToken);
        return Ok(antwort);
    }

    /// <summary>Rotiert das Aktualisierungstoken und stellt ein neues Zugriffstoken aus.</summary>
    [HttpPost("token-erneuern")]
    [AllowAnonymous]
    [EnableRateLimiting("authentifizierung")]
    [ProducesResponseType<AnmeldeAntwort>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AnmeldeAntwort>> TokenErneuern(
        AktualisierungstokenAnfrage anfrage,
        CancellationToken abbrechToken) =>
        Ok(await _sender.Send(new ZugriffstokenErneuern(anfrage.Aktualisierungstoken), abbrechToken));

    /// <summary>Widerruft den übermittelten Aktualisierungstoken.</summary>
    [HttpPost("abmelden")]
    [AllowAnonymous]
    [EnableRateLimiting("authentifizierung")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Abmelden(
        AktualisierungstokenAnfrage anfrage,
        CancellationToken abbrechToken)
    {
        await _sender.Send(new Abmelden(anfrage.Aktualisierungstoken), abbrechToken);
        return NoContent();
    }

    /// <summary>Gibt das Profil des angemeldeten Kunden zurück.</summary>
    [HttpGet("profil")]
    [Authorize]
    [ProducesResponseType<KundenProfilDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KundenProfilDto>> EigenesProfil(CancellationToken abbrechToken)
    {
        var kundeId = BenutzerId();
        var profil = await _sender.Send(new EigenesProfilAbfragen(kundeId), abbrechToken);
        return profil is null ? NotFound() : Ok(profil);
    }

    private Guid BenutzerId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
