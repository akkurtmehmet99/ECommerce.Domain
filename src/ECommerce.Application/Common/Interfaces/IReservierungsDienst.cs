namespace ECommerce.Application.Common.Interfaces;

public interface IReservierungsDienst
{
    Task<bool> AbgelaufeneBestellungFreigebenAsync(
        Guid bestellungId,
        CancellationToken abbrechToken);
}
