using System.Security.Claims;
using ECommerce.API.Contracts;
using ECommerce.Application.Common.DTOs;
using ECommerce.Application.Features.Bestellungen;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>Bestellungen des Kunden und administrative Bestellabwicklung.</summary>
[ApiController]
[Authorize]
[Route("api/bestellungen")]
[Produces("application/json")]
public sealed class BestellungenController : ControllerBase
{
    private readonly ISender _sender;

    public BestellungenController(ISender sender) => _sender = sender;

    /// <summary>Erstellt aus dem Warenkorb eine Bestellung und reserviert den Lagerbestand.</summary>
    [HttpPost]
    [ProducesResponseType<BestellungDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BestellungDto>> Erstellen(
        BestellungAnfrage anfrage,
        CancellationToken abbrechToken)
    {
        var bestellung = await _sender.Send(
            new BestellungErstellen(
                BenutzerId(),
                anfrage.Liefername,
                anfrage.Lieferstrasse,
                anfrage.LieferPostleitzahl,
                anfrage.LieferStadt,
                anfrage.LieferLand),
            abbrechToken);
        return CreatedAtAction(nameof(NachIdAbfragen), new { id = bestellung.Id }, bestellung);
    }

    /// <summary>Listet die Bestellungen des angemeldeten Kunden auf.</summary>
    [HttpGet]
    [ProducesResponseType<Seitenergebnis<BestellungDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<Seitenergebnis<BestellungDto>>> MeineBestellungen(
        [FromQuery] int seite = 1,
        [FromQuery] int seitengroesse = 20,
        CancellationToken abbrechToken = default) =>
        Ok(await _sender.Send(
            new MeineBestellungenAbfragen(BenutzerId(), seite, seitengroesse),
            abbrechToken));

    /// <summary>Listet Bestellungen aller Kunden auf. Nur Administratoren haben Zugriff.</summary>
    [HttpGet("verwaltung")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType<Seitenergebnis<BestellungDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Seitenergebnis<BestellungDto>>> AlleBestellungen(
        [FromQuery] int seite = 1,
        [FromQuery] int seitengroesse = 20,
        CancellationToken abbrechToken = default) =>
        Ok(await _sender.Send(
            new AlleBestellungenAbfragen(seite, seitengroesse),
            abbrechToken));

    /// <summary>Gibt eine eigene Bestellung zurück.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<BestellungDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BestellungDto>> NachIdAbfragen(
        Guid id,
        CancellationToken abbrechToken)
    {
        var bestellung = await _sender.Send(
            new BestellungNachIdAbfragen(BenutzerId(), id),
            abbrechToken);
        return bestellung is null ? NotFound() : Ok(bestellung);
    }

    /// <summary>Ändert den Status einer Bestellung. Nur Administratoren dürfen diese Aktion ausführen.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StatusAendern(
        Guid id,
        Bestellstatus status,
        CancellationToken abbrechToken)
    {
        var aktualisiert = await _sender.Send(
            new BestellstatusAendern(id, status),
            abbrechToken);
        return aktualisiert ? NoContent() : NotFound();
    }

    private Guid BenutzerId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
