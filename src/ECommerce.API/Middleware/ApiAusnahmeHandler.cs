using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ECommerce.API.Middleware;

public sealed class ApiAusnahmeHandler(ILogger<ApiAusnahmeHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpKontext,
        Exception ausnahme,
        CancellationToken abbrechToken)
    {
        var (status, titel, detail) = ausnahme switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Ungültige Anfrage.",
                "Bitte überprüfen Sie die übermittelten Angaben."),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Nicht autorisiert.",
                "Die Anmeldung ist ungültig oder abgelaufen."),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Ressource nicht gefunden.",
                "Die angeforderte Ressource wurde nicht gefunden."),
            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "Anfragekonflikt.",
                "Die Änderung verletzt eine Datenbankbedingung oder steht im Konflikt mit vorhandenen Daten."),
            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "Anfragekonflikt.",
                ausnahme.Message),
            _ => (0, string.Empty, string.Empty)
        };

        if (status == 0)
        {
            logger.LogError(ausnahme, "Unbehandelte Ausnahme bei der API-Anfrage.");
            return false;
        }

        logger.LogWarning(ausnahme, "API-Anfrage wurde mit HTTP {Status} abgewiesen.", status);
        var problem = new ProblemDetails
        {
            Status = status,
            Title = titel,
            Detail = detail,
            Instance = httpKontext.Request.Path
        };

        if (ausnahme is ValidationException validierungsAusnahme)
        {
            problem.Extensions["errors"] = validierungsAusnahme.Errors
                .GroupBy(fehler => fehler.PropertyName)
                .ToDictionary(
                    gruppe => gruppe.Key,
                    gruppe => gruppe.Select(fehler => fehler.ErrorMessage).ToArray());
        }

        httpKontext.Response.StatusCode = status;
        httpKontext.Response.ContentType = "application/problem+json; charset=utf-8";
        await JsonSerializer.SerializeAsync(
            httpKontext.Response.Body,
            problem,
            cancellationToken: abbrechToken);
        return true;
    }
}
