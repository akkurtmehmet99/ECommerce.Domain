using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var verbindungszeichenfolge = configuration.GetConnectionString("ECommerce")
            ?? throw new InvalidOperationException(
                "Die Verbindungszeichenfolge 'ECommerce' wurde nicht konfiguriert.");

        var sqliteVerbindungszeichenfolge =
            new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(verbindungszeichenfolge);

        if (!Path.IsPathRooted(sqliteVerbindungszeichenfolge.DataSource))
        {
            sqliteVerbindungszeichenfolge.DataSource =
                Path.Combine(contentRootPath, sqliteVerbindungszeichenfolge.DataSource);
        }
        sqliteVerbindungszeichenfolge.DefaultTimeout = 30;

        services.AddDbContext<AnwendungDbContext>(optionen =>
            optionen.UseSqlite(sqliteVerbindungszeichenfolge.ToString()));
        services.AddScoped<IApplicationDbContext>(anbieter =>
            anbieter.GetRequiredService<AnwendungDbContext>());
        services.AddScoped<IPasswordHasher<Kunde>, PasswordHasher<Kunde>>();
        services.AddScoped<ITokenDienst, JwtTokenDienst>();
        services.AddScoped<IReservierungsDienst, ReservierungsDienst>();
        services.AddHostedService<ReservierungsHintergrunddienst>();

        return services;
    }
}
