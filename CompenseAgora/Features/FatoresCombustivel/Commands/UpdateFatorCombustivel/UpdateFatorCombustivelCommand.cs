using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.FatoresCombustivel.Commands.UpdateFatorCombustivel;

public record UpdateFatorCombustivelCommand(
    int Codigo,
    int CodigoCombustivel,
    int Ano,
    decimal PoderCalorificoInferior,
    decimal Densidade,
    decimal CO2,
    decimal CH4,
    decimal N2O) : IRequest;

public class UpdateFatorCombustivelCommandValidator : AbstractValidator<UpdateFatorCombustivelCommand>
{
    public UpdateFatorCombustivelCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
        RuleFor(x => x.CodigoCombustivel).GreaterThan(0).WithMessage("Combustível é obrigatório.");
        RuleFor(x => x.Ano).GreaterThan(1900).WithMessage("Ano inválido.");
        RuleFor(x => x.PoderCalorificoInferior).GreaterThanOrEqualTo(0).WithMessage("Poder calorífico inferior deve ser maior ou igual a zero.");
        RuleFor(x => x.Densidade).GreaterThanOrEqualTo(0).WithMessage("Densidade deve ser maior ou igual a zero.");
        RuleFor(x => x.CO2).GreaterThanOrEqualTo(0).WithMessage("CO2 deve ser maior ou igual a zero.");
        RuleFor(x => x.CH4).GreaterThanOrEqualTo(0).WithMessage("CH4 deve ser maior ou igual a zero.");
        RuleFor(x => x.N2O).GreaterThanOrEqualTo(0).WithMessage("N2O deve ser maior ou igual a zero.");
    }
}

public class UpdateFatorCombustivelCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<UpdateFatorCombustivelCommand>
{
    public async Task Handle(UpdateFatorCombustivelCommand request, CancellationToken cancellationToken)
    {
        var fator = await dbContext.FatoresDoCombustivel.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(FatorCombustivel), request.Codigo);

        fator.CodigoCombustivel = request.CodigoCombustivel;
        fator.Ano = request.Ano;
        fator.PoderCalorificoInferior = request.PoderCalorificoInferior;
        fator.Densidade = request.Densidade;
        fator.CO2 = request.CO2;
        fator.CH4 = request.CH4;
        fator.N2O = request.N2O;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
