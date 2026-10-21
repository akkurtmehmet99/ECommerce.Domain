using ECommerce.API.Contracts;
using ECommerce.Application.Features.Kategorien;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>Öffentliche Produktkategorien und administrative Kategorieverwaltung.</summary>
[ApiController]
[Route("api/kategorien")]
[Produces("application/json")]
public sealed class KategorienController : ControllerBase
{
    private readonly ISender _sender;

    public KategorienController(ISender sender) => _sender = sender;

    /// <summary>Listet alle aktiven Produktkategorien auf.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<KategorieDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<KategorieDto>>> AlleAbfragen(
        CancellationToken abbrechToken) =>
        Ok(await _sender.Send(new KategorienAbfragen(), abbrechToken));

    /// <summary>Legt eine Kategorie an. Nur Administratoren dürfen diese Aktion ausführen.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType<KategorieDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<KategorieDto>> Erstellen(
        KategorieAnfrage anfrage,
        CancellationToken abbrechToken)
    {
        var id = await _sender.Send(
            new KategorieErstellen(anfrage.Name, anfrage.Beschreibung),
            abbrechToken);
        var antwort = new KategorieDto(id, anfrage.Name.Trim(), anfrage.Beschreibung.Trim());
        return Created("/api/kategorien", antwort);
    }

    /// <summary>Ändert eine Kategorie. Nur Administratoren dürfen diese Aktion ausführen.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Aktualisieren(
        Guid id,
        KategorieAnfrage anfrage,
        CancellationToken abbrechToken)
    {
        var aktualisiert = await _sender.Send(
            new KategorieAktualisieren(id, anfrage.Name, anfrage.Beschreibung),
            abbrechToken);
        return aktualisiert ? NoContent() : NotFound();
    }

    /// <summary>Deaktiviert eine Kategorie ohne zugehörige Produkte.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Loeschen(Guid id, CancellationToken abbrechToken)
    {
        var geloescht = await _sender.Send(new KategorieLoeschen(id), abbrechToken);
        return geloescht ? NoContent() : NotFound();
    }
}
