namespace ECommerce.Application.Common.Interfaces;

public interface ITokenDienst
{
    (string Token, DateTimeOffset Ablaufzeitpunkt) Erstellen(Guid kundeId, string email, string rolle);
    string AktualisierungstokenErstellen();
    string AktualisierungstokenHashen(string token);
}
