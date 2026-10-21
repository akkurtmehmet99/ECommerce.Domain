using System.Security.Claims;
using ECommerce.API.Contracts;
using ECommerce.Application.Features.Zahlungen;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>Simulierte Zahlungen für die Entwicklungs- und Testumgebung.</summary>
[ApiController]
[Authorize]
[Route("api/zahlungen")]
[Produces("application/json")]
public sealed class ZahlungenController : ControllerBase
{
    private readonly ISender _sender;

    public ZahlungenController(ISender sender) => _sender = sender;

    /// <summary>Simuliert eine Zahlung. Dieser Endpunkt verarbeitet keine echten Zahlungsdaten.</summary>
    [HttpPost("{bestellungId:guid}/sandbox")]
    [ProducesResponseType<ZahlungDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ZahlungDto>> SandboxAusfuehren(
        Guid bestellungId,
        SandboxZahlungAnfrage anfrage,
        CancellationToken abbrechToken) =>
        Ok(await _sender.Send(
            new SandboxZahlungAusfuehren(BenutzerId(), bestellungId, anfrage.Erfolgreich),
            abbrechToken));

    private Guid BenutzerId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
