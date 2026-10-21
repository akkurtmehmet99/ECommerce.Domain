using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using ECommerce.Application;
using ECommerce.API.Middleware;
using ECommerce.Domain.Entities;
using ECommerce.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var signaturschluessel = builder.Configuration["Authentifizierung:Signaturschluessel"];
if (string.IsNullOrWhiteSpace(signaturschluessel) && builder.Environment.IsDevelopment())
{
    signaturschluessel = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
    builder.Configuration["Authentifizierung:Signaturschluessel"] = signaturschluessel;
    Console.Error.WriteLine(
        "WARNUNG: Es wurde ein temporärer JWT-Schlüssel für die Entwicklung erzeugt. " +
        "Tokens werden nach einem Neustart ungültig; konfigurieren Sie für dauerhafte lokale Tests einen Secret Store.");
}

if (string.IsNullOrWhiteSpace(signaturschluessel))
{
    throw new InvalidOperationException(
        "Authentifizierung:Signaturschluessel muss über Umgebungsvariablen oder einen Secret Store konfiguriert werden.");
}
if (Encoding.UTF8.GetByteCount(signaturschluessel) < 32)
{
    throw new InvalidOperationException("Der JWT-Signaturschlüssel muss mindestens 32 UTF-8-Bytes lang sein.");
}

var aussteller = builder.Configuration["Authentifizierung:Aussteller"] ?? "ECommerce";
var zielgruppe = builder.Configuration["Authentifizierung:Zielgruppe"] ?? "ECommerceKunden";

builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddControllers()
    .AddJsonOptions(optionen =>
        optionen.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(optionen =>
    {
        optionen.InvalidModelStateResponseFactory = kontext =>
        {
            var problem = new Microsoft.AspNetCore.Mvc.ValidationProblemDetails(kontext.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ungültige Anfrage.",
                Detail = "Bitte überprüfen Sie die übermittelten Angaben.",
                Instance = kontext.HttpContext.Request.Path
            };
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problem);
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(optionen =>
{
    var xmlDatei = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    optionen.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlDatei));
    optionen.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT-Zugriffstoken als Bearer-Token."
    });
    optionen.OperationFilter<BearerOperationFilter>();
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiAusnahmeHandler>();
builder.Services.AddHealthChecks()
    .AddCheck<DatenbankGesundheitspruefung>("datenbank");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(optionen =>
    {
        optionen.MapInboundClaims = false;
        optionen.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signaturschluessel)),
            ValidateIssuer = true,
            ValidIssuer = aussteller,
            ValidateAudience = true,
            ValidAudience = zielgruppe,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(optionen =>
{
    optionen.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    optionen.AddPolicy("authentifizierung", httpKontext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpKontext.Connection.RemoteIpAddress?.ToString() ?? "unbekannt",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(async (StatusCodeContext statusKontext) =>
{
    var status = statusKontext.HttpContext.Response.StatusCode;
    statusKontext.HttpContext.Response.ContentType = "application/problem+json; charset=utf-8";
    await System.Text.Json.JsonSerializer.SerializeAsync(
        statusKontext.HttpContext.Response.Body,
        new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = status,
            Title = status switch
            {
                StatusCodes.Status401Unauthorized => "Nicht autorisiert.",
                StatusCodes.Status403Forbidden => "Zugriff verweigert.",
                StatusCodes.Status404NotFound => "Ressource nicht gefunden.",
                StatusCodes.Status429TooManyRequests => "Zu viele Anfragen.",
                _ => "Die Anfrage konnte nicht verarbeitet werden."
            },
            Instance = statusKontext.HttpContext.Request.Path
        },
        cancellationToken: statusKontext.HttpContext.RequestAborted);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseHealthChecks("/gesundheit");
app.MapControllers();

await using (var bereich = app.Services.CreateAsyncScope())
{
    var dbKontext = bereich.ServiceProvider.GetRequiredService<AnwendungDbContext>();
    await dbKontext.Database.MigrateAsync();
    await AdministratorInitialisierung.AdministratorErstellenWennKonfiguriertAsync(
        dbKontext,
        app.Configuration,
        bereich.ServiceProvider.GetRequiredService<IPasswordHasher<Kunde>>());
}

app.Run();

public partial class Program { }
