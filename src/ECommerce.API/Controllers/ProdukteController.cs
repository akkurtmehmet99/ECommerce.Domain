using ECommerce.API.Contracts;
using ECommerce.Application.Common.DTOs;
using ECommerce.Application.Features.Produkte;
using ECommerce.Application.Features.Produkte.Commands.ProduktErstellen;
using ECommerce.Application.Features.Produkte.DTOs;
using ECommerce.Application.Features.Produkte.Queries.ProduktListeAbfragen;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>Öffentlicher Produktkatalog und administrative Produktverwaltung.</summary>
[ApiController]
[Route("api/produkte")]
[Produces("application/json")]
public sealed class ProdukteController : ControllerBase
{
    private readonly ISender _sender;

    public ProdukteController(ISender sender) => _sender = sender;

    /// <summary>Listet alle aktiven Produkte auf.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<Seitenergebnis<ProduktDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Seitenergebnis<ProduktDto>>> AlleAbfragen(
        [FromQuery] int seite = 1,
        [FromQuery] int seitengroesse = 20,
        CancellationToken abbrechToken = default) =>
        Ok(await _sender.Send(new ProduktListeAbfragenQuery(seite, seitengroesse), abbrechToken));

    /// <summary>Gibt ein aktives Produkt zurück.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<ProduktDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProduktDto>> NachIdAbfragen(
        Guid id,
        CancellationToken abbrechToken)
    {
        var produkt = await _sender.Send(new ProduktNachIdAbfragen(id), abbrechToken);
        return produkt is null ? NotFound() : Ok(produkt);
    }

    /// <summary>Erstellt ein Produkt. Nur Administratoren dürfen diese Aktion ausführen.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType<ProduktDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProduktDto>> Erstellen(
        ProduktAnfrage anfrage,
        CancellationToken abbrechToken)
    {
        var id = await _sender.Send(
            new ProduktErstellenCommand(
                anfrage.Name, anfrage.Beschreibung, anfrage.Preis, anfrage.Lagerbestand, anfrage.KategorieId),
            abbrechToken);
        var produkt = await _sender.Send(new ProduktNachIdAbfragen(id), abbrechToken);
        return CreatedAtAction(nameof(NachIdAbfragen), new { id }, produkt);
    }

    /// <summary>Ändert Produktdaten. Nur Administratoren dürfen diese Aktion ausführen.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Aktualisieren(
        Guid id,
        ProduktAnfrage anfrage,
        CancellationToken abbrechToken)
    {
        var aktualisiert = await _sender.Send(
            new ProduktAktualisieren(
                id, anfrage.Name, anfrage.Beschreibung, anfrage.Preis, anfrage.Lagerbestand, anfrage.KategorieId),
            abbrechToken);
        return aktualisiert ? NoContent() : NotFound();
    }

    /// <summary>Deaktiviert ein Produkt, ohne alte Bestellinformationen zu löschen.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deaktivieren(Guid id, CancellationToken abbrechToken)
    {
        var deaktiviert = await _sender.Send(new ProduktDeaktivieren(id), abbrechToken);
        return deaktiviert ? NoContent() : NotFound();
    }
}
