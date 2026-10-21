using System.Security.Claims;
using ECommerce.API.Contracts;
using ECommerce.Application.Features.Warenkorb;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>Warenkorb des angemeldeten Kunden.</summary>
[ApiController]
[Authorize]
[Route("api/warenkorb")]
[Produces("application/json")]
public sealed class WarenkorbController : ControllerBase
{
    private readonly ISender _sender;

    public WarenkorbController(ISender sender) => _sender = sender;

    /// <summary>Gibt den eigenen Warenkorb zurück.</summary>
    [HttpGet]
    [ProducesResponseType<WarenkorbDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WarenkorbDto>> Abfragen(CancellationToken abbrechToken) =>
        Ok(await _sender.Send(new WarenkorbAbfragen(BenutzerId()), abbrechToken));

    /// <summary>Fügt ein Produkt hinzu oder erhöht dessen vorhandene Menge.</summary>
    [HttpPost("positionen/{produktId:guid}")]
    [ProducesResponseType<WarenkorbDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WarenkorbDto>> Hinzufuegen(
        Guid produktId,
        WarenkorbMengeAnfrage anfrage,
        CancellationToken abbrechToken) =>
        Ok(await _sender.Send(
            new WarenkorbpositionHinzufuegen(BenutzerId(), produktId, anfrage.Menge),
            abbrechToken));

    /// <summary>Setzt die Menge einer Warenkorbposition neu.</summary>
    [HttpPut("positionen/{positionId:guid}")]
    [ProducesResponseType<WarenkorbDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WarenkorbDto>> MengeAendern(
        Guid positionId,
        WarenkorbMengeAnfrage anfrage,
        CancellationToken abbrechToken) =>
        Ok(await _sender.Send(
            new WarenkorbMengeAendern(BenutzerId(), positionId, anfrage.Menge),
            abbrechToken));

    /// <summary>Entfernt eine Position aus dem eigenen Warenkorb.</summary>
    [HttpDelete("positionen/{positionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Entfernen(Guid positionId, CancellationToken abbrechToken)
    {
        var entfernt = await _sender.Send(
            new WarenkorbpositionEntfernen(BenutzerId(), positionId),
            abbrechToken);
        return entfernt ? NoContent() : NotFound();
    }

    private Guid BenutzerId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
