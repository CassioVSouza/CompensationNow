using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.FatoresCombustivel.Commands.CreateFatorCombustivel;

public record CreateFatorCombustivelCommand(
    int CodigoCombustivel,
    int Ano,
    decimal PoderCalorificoInferior,
    decimal Densidade,
    decimal CO2,
    decimal CH4,
    decimal N2O) : IRequest<int>;

public class CreateFatorCombustivelCommandValidator : AbstractValidator<CreateFatorCombustivelCommand>
{
    public CreateFatorCombustivelCommandValidator()
    {
        RuleFor(x => x.CodigoCombustivel).GreaterThan(0).WithMessage("Combustível é obrigatório.");
        RuleFor(x => x.Ano).GreaterThan(1900).WithMessage("Ano inválido.");
        RuleFor(x => x.PoderCalorificoInferior).GreaterThanOrEqualTo(0).WithMessage("Poder calorífico inferior deve ser maior ou igual a zero.");
        RuleFor(x => x.Densidade).GreaterThanOrEqualTo(0).WithMessage("Densidade deve ser maior ou igual a zero.");
        RuleFor(x => x.CO2).GreaterThanOrEqualTo(0).WithMessage("CO2 deve ser maior ou igual a zero.");
        RuleFor(x => x.CH4).GreaterThanOrEqualTo(0).WithMessage("CH4 deve ser maior ou igual a zero.");
        RuleFor(x => x.N2O).GreaterThanOrEqualTo(0).WithMessage("N2O deve ser maior ou igual a zero.");
    }
}

public class CreateFatorCombustivelCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<CreateFatorCombustivelCommand, int>
{
    public async Task<int> Handle(CreateFatorCombustivelCommand request, CancellationToken cancellationToken)
    {
        var fator = new FatorCombustivel
        {
            CodigoCombustivel = request.CodigoCombustivel,
            Ano = request.Ano,
            PoderCalorificoInferior = request.PoderCalorificoInferior,
            Densidade = request.Densidade,
            CO2 = request.CO2,
            CH4 = request.CH4,
            N2O = request.N2O,
        };

        dbContext.FatoresDoCombustivel.Add(fator);
        await dbContext.SaveChangesAsync(cancellationToken);

        return fator.Codigo;
    }
}
