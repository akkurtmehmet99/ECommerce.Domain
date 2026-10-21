using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Authentifizierung;

public sealed record RegistrierenBefehl(
    string Vorname,
    string Nachname,
    string EMail,
    string Passwort) : IRequest<AnmeldeAntwort>;

public sealed record AnmeldenBefehl(string EMail, string Passwort) : IRequest<AnmeldeAntwort>;
public sealed record ZugriffstokenErneuern(string Aktualisierungstoken) : IRequest<AnmeldeAntwort>;
public sealed record Abmelden(string Aktualisierungstoken) : IRequest<bool>;

/// <summary>Öffentliches Kundenprofil ohne vertrauliche Zugangsdaten.</summary>
public sealed record KundenProfilDto(Guid Id, string Vorname, string Nachname, string EMail, string Rolle);

public sealed record EigenesProfilAbfragen(Guid KundeId) : IRequest<KundenProfilDto?>;

public sealed class EigenesProfilHandler : IRequestHandler<EigenesProfilAbfragen, KundenProfilDto?>
{
    private readonly IApplicationDbContext _kontext;

    public EigenesProfilHandler(IApplicationDbContext kontext) => _kontext = kontext;

    public async Task<KundenProfilDto?> Handle(
        EigenesProfilAbfragen abfrage,
        CancellationToken abbrechToken)
    {
        return await _kontext.Kunden.AsNoTracking()
            .Where(kunde => kunde.Id == abfrage.KundeId && kunde.Aktiv && !kunde.Geloescht)
            .Select(kunde => new KundenProfilDto(
                kunde.Id, kunde.Vorname, kunde.Nachname, kunde.EMail, kunde.Rolle))
            .SingleOrDefaultAsync(abbrechToken);
    }
}

/// <summary>Antwort mit kurzlebigem Zugriffstoken nach einer erfolgreichen Anmeldung.</summary>
public sealed record AnmeldeAntwort(
    Guid KundeId,
    string Vorname,
    string Nachname,
    string EMail,
    string Rolle,
    string Zugriffstoken,
    string Aktualisierungstoken,
    DateTimeOffset Ablaufzeitpunkt);

public sealed class RegistrierenBefehlValidator : AbstractValidator<RegistrierenBefehl>
{
    public RegistrierenBefehlValidator()
    {
        RuleFor(befehl => befehl.Vorname).NotEmpty().MaximumLength(100);
        RuleFor(befehl => befehl.Nachname).NotEmpty().MaximumLength(100);
        RuleFor(befehl => befehl.EMail).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(befehl => befehl.Passwort).NotEmpty().MinimumLength(12)
            .MaximumLength(256)
            .Matches("[A-Z]").WithMessage("Das Passwort muss mindestens einen Großbuchstaben enthalten.")
            .Matches("[a-z]").WithMessage("Das Passwort muss mindestens einen Kleinbuchstaben enthalten.")
            .Matches("[0-9]").WithMessage("Das Passwort muss mindestens eine Zahl enthalten.");
    }
}

public sealed class AnmeldenBefehlValidator : AbstractValidator<AnmeldenBefehl>
{
    public AnmeldenBefehlValidator()
    {
        RuleFor(befehl => befehl.EMail).NotEmpty().EmailAddress();
        RuleFor(befehl => befehl.Passwort).NotEmpty().MaximumLength(256);
    }
}

public sealed class RegistrierenHandler : IRequestHandler<RegistrierenBefehl, AnmeldeAntwort>
{
    private readonly IApplicationDbContext _kontext;
    private readonly IPasswordHasher<Kunde> _passwortHasher;
    private readonly ITokenDienst _tokenDienst;

    public RegistrierenHandler(
        IApplicationDbContext kontext,
        IPasswordHasher<Kunde> passwortHasher,
        ITokenDienst tokenDienst)
    {
        _kontext = kontext;
        _passwortHasher = passwortHasher;
        _tokenDienst = tokenDienst;
    }

    public async Task<AnmeldeAntwort> Handle(
        RegistrierenBefehl befehl,
        CancellationToken abbrechToken)
    {
        var email = befehl.EMail.Trim().ToLowerInvariant();
        if (await _kontext.Kunden.AnyAsync(kunde => kunde.EMail == email, abbrechToken))
        {
            throw new InvalidOperationException("Für diese E-Mail-Adresse existiert bereits ein Konto.");
        }

        var kunde = new Kunde
        {
            Vorname = befehl.Vorname.Trim(),
            Nachname = befehl.Nachname.Trim(),
            EMail = email,
            Rolle = "Kunde"
        };
        kunde.PasswortHash = _passwortHasher.HashPassword(kunde, befehl.Passwort);
        kunde.Warenkorb = new ECommerce.Domain.Entities.Warenkorb { Kunde = kunde };
        _kontext.Kunden.Add(kunde);
        var (sitzung, aktualisierungstoken) = SitzungAnlegen(kunde);
        _kontext.Sitzungstoken.Add(sitzung);
        await _kontext.SaveChangesAsync(abbrechToken);

        return AntwortErstellen(kunde, aktualisierungstoken);
    }

    private (Sitzungstoken Sitzung, string KlartextToken) SitzungAnlegen(Kunde kunde)
    {
        var token = _tokenDienst.AktualisierungstokenErstellen();
        var sitzung = new Sitzungstoken
        {
            Kunde = kunde,
            Hash = _tokenDienst.AktualisierungstokenHashen(token),
            Ablaufzeitpunkt = DateTimeOffset.UtcNow.AddDays(30)
        };
        return (sitzung, token);
    }

    private AnmeldeAntwort AntwortErstellen(Kunde kunde, string aktualisierungstoken)
    {
        var (token, ablaufzeitpunkt) = _tokenDienst.Erstellen(kunde.Id, kunde.EMail, kunde.Rolle);
        return new AnmeldeAntwort(
            kunde.Id, kunde.Vorname, kunde.Nachname, kunde.EMail, kunde.Rolle,
            token, aktualisierungstoken, ablaufzeitpunkt);
    }
}

public sealed class AnmeldenHandler : IRequestHandler<AnmeldenBefehl, AnmeldeAntwort>
{
    private readonly IApplicationDbContext _kontext;
    private readonly IPasswordHasher<Kunde> _passwortHasher;
    private readonly ITokenDienst _tokenDienst;

    public AnmeldenHandler(
        IApplicationDbContext kontext,
        IPasswordHasher<Kunde> passwortHasher,
        ITokenDienst tokenDienst)
    {
        _kontext = kontext;
        _passwortHasher = passwortHasher;
        _tokenDienst = tokenDienst;
    }

    public async Task<AnmeldeAntwort> Handle(AnmeldenBefehl befehl, CancellationToken abbrechToken)
    {
        var email = befehl.EMail.Trim().ToLowerInvariant();
        var kunde = await _kontext.Kunden
            .SingleOrDefaultAsync(
                benutzer => benutzer.EMail == email && benutzer.Aktiv && !benutzer.Geloescht,
                abbrechToken);

        if (kunde is null)
        {
            throw new UnauthorizedAccessException("E-Mail-Adresse oder Passwort ist ungültig.");
        }

        var pruefergebnis = _passwortHasher.VerifyHashedPassword(kunde, kunde.PasswortHash, befehl.Passwort);
        if (pruefergebnis == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("E-Mail-Adresse oder Passwort ist ungültig.");
        }

        if (pruefergebnis == PasswordVerificationResult.SuccessRehashNeeded)
        {
            kunde.PasswortHash = _passwortHasher.HashPassword(kunde, befehl.Passwort);
            await _kontext.SaveChangesAsync(abbrechToken);
        }

        var (token, ablaufzeitpunkt) = _tokenDienst.Erstellen(kunde.Id, kunde.EMail, kunde.Rolle);
        var aktualisierungstoken = _tokenDienst.AktualisierungstokenErstellen();
        _kontext.Sitzungstoken.Add(new Sitzungstoken
        {
            KundeId = kunde.Id,
            Hash = _tokenDienst.AktualisierungstokenHashen(aktualisierungstoken),
            Ablaufzeitpunkt = DateTimeOffset.UtcNow.AddDays(30)
        });
        await _kontext.SaveChangesAsync(abbrechToken);

        return new AnmeldeAntwort(
            kunde.Id, kunde.Vorname, kunde.Nachname, kunde.EMail, kunde.Rolle,
            token, aktualisierungstoken, ablaufzeitpunkt);
    }
}

public sealed class ZugriffstokenErneuernValidator : AbstractValidator<ZugriffstokenErneuern>
{
    public ZugriffstokenErneuernValidator() =>
        RuleFor(befehl => befehl.Aktualisierungstoken).NotEmpty().Length(40, 128);
}

public sealed class AbmeldenValidator : AbstractValidator<Abmelden>
{
    public AbmeldenValidator() =>
        RuleFor(befehl => befehl.Aktualisierungstoken).NotEmpty().Length(40, 128);
}

public sealed class SitzungHandler :
    IRequestHandler<ZugriffstokenErneuern, AnmeldeAntwort>,
    IRequestHandler<Abmelden, bool>
{
    private readonly IApplicationDbContext _kontext;
    private readonly ITokenDienst _tokenDienst;

    public SitzungHandler(IApplicationDbContext kontext, ITokenDienst tokenDienst)
    {
        _kontext = kontext;
        _tokenDienst = tokenDienst;
    }

    public async Task<AnmeldeAntwort> Handle(
        ZugriffstokenErneuern befehl,
        CancellationToken abbrechToken)
    {
        await using var transaktion = await _kontext.Database.BeginTransactionAsync(abbrechToken);
        var hash = _tokenDienst.AktualisierungstokenHashen(befehl.Aktualisierungstoken);
        var sitzung = await _kontext.Sitzungstoken
            .Include(token => token.Kunde)
            .SingleOrDefaultAsync(token => token.Hash == hash, abbrechToken);
        if (sitzung is null || sitzung.Ablaufzeitpunkt <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("Das Aktualisierungstoken ist ungültig oder abgelaufen.");
        }

        if (sitzung.WiderrufenAm is not null)
        {
            var jetzt = DateTimeOffset.UtcNow;
            await _kontext.Sitzungstoken
                .Where(token => token.KundeId == sitzung.KundeId && token.WiderrufenAm == null)
                .ExecuteUpdateAsync(
                    aktualisierung => aktualisierung.SetProperty(
                        token => token.WiderrufenAm,
                        jetzt),
                    abbrechToken);
            await transaktion.CommitAsync(abbrechToken);
            throw new UnauthorizedAccessException(
                "Ein bereits verwendetes Aktualisierungstoken wurde erkannt. Bitte melden Sie sich erneut an.");
        }

        if (!sitzung.Kunde.Aktiv || sitzung.Kunde.Geloescht)
        {
            throw new UnauthorizedAccessException("Das Benutzerkonto ist deaktiviert.");
        }

        var neuerKlartextToken = _tokenDienst.AktualisierungstokenErstellen();
        var neuerHash = _tokenDienst.AktualisierungstokenHashen(neuerKlartextToken);
        sitzung.WiderrufenAm = DateTimeOffset.UtcNow;
        sitzung.ErsetztDurchHash = neuerHash;
        _kontext.Sitzungstoken.Add(new Sitzungstoken
        {
            KundeId = sitzung.KundeId,
            Hash = neuerHash,
            Ablaufzeitpunkt = DateTimeOffset.UtcNow.AddDays(30)
        });

        await _kontext.SaveChangesAsync(abbrechToken);
        await transaktion.CommitAsync(abbrechToken);

        var (zugriffstoken, ablaufzeitpunkt) =
            _tokenDienst.Erstellen(sitzung.Kunde.Id, sitzung.Kunde.EMail, sitzung.Kunde.Rolle);
        return new AnmeldeAntwort(
            sitzung.Kunde.Id,
            sitzung.Kunde.Vorname,
            sitzung.Kunde.Nachname,
            sitzung.Kunde.EMail,
            sitzung.Kunde.Rolle,
            zugriffstoken,
            neuerKlartextToken,
            ablaufzeitpunkt);
    }

    public async Task<bool> Handle(Abmelden befehl, CancellationToken abbrechToken)
    {
        var hash = _tokenDienst.AktualisierungstokenHashen(befehl.Aktualisierungstoken);
        var sitzung = await _kontext.Sitzungstoken
            .SingleOrDefaultAsync(token => token.Hash == hash, abbrechToken);
        if (sitzung is null || sitzung.WiderrufenAm is not null)
        {
            return false;
        }

        sitzung.WiderrufenAm = DateTimeOffset.UtcNow;
        await _kontext.SaveChangesAsync(abbrechToken);
        return true;
    }
}
