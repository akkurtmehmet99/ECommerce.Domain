using FluentValidation;

namespace ECommerce.Application.Features.Produkte.Commands.ProduktErstellen;

public class ProduktErstellenCommandValidator : AbstractValidator<ProduktErstellenCommand>
{
    public ProduktErstellenCommandValidator()
    {
        RuleFor(produkt => produkt.Name)
            .NotEmpty().WithMessage("Der Produktname darf nicht leer sein.")
            .MinimumLength(2).WithMessage("Der Produktname muss mindestens 2 Zeichen lang sein.")
            .MaximumLength(100).WithMessage("Der Produktname darf maximal 100 Zeichen lang sein.");

        RuleFor(produkt => produkt.Beschreibung)
            .NotNull().WithMessage("Die Beschreibung darf nicht fehlen.")
            .MaximumLength(500).WithMessage("Die Beschreibung darf maximal 500 Zeichen lang sein.");

        RuleFor(produkt => produkt.Preis)
            .GreaterThan(0).WithMessage("Der Preis muss größer als 0 sein.")
            .PrecisionScale(18, 2, false).WithMessage("Der Preis darf maximal 2 Nachkommastellen haben.");

        RuleFor(produkt => produkt.Lagerbestand)
            .GreaterThanOrEqualTo(0).WithMessage("Der Lagerbestand darf nicht negativ sein.");

        RuleFor(produkt => produkt.KategorieId)
            .NotEmpty().WithMessage("Eine Kategorie muss ausgewählt werden.");
    }
}
