using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.FatoresComposicaoCombustivel.Commands.CreateFatorComposicaoCombustivel;

public record CreateFatorComposicaoCombustivelCommand(
    int Mes,
    int Ano,
    decimal PercentualEtanol,
    decimal PercentualBiodiesel) : IRequest<int>;

public class CreateFatorComposicaoCombustivelCommandValidator : AbstractValidator<CreateFatorComposicaoCombustivelCommand>
{
    public CreateFatorComposicaoCombustivelCommandValidator()
    {
        RuleFor(x => x.Mes).InclusiveBetween(1, 12).WithMessage("Mês deve estar entre 1 e 12.");
        RuleFor(x => x.Ano).GreaterThan(1900).WithMessage("Ano inválido.");
        RuleFor(x => x.PercentualEtanol).InclusiveBetween(0, 100).WithMessage("Percentual de etanol deve estar entre 0 e 100.");
        RuleFor(x => x.PercentualBiodiesel).InclusiveBetween(0, 100).WithMessage("Percentual de biodiesel deve estar entre 0 e 100.");
    }
}

public class CreateFatorComposicaoCombustivelCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<CreateFatorComposicaoCombustivelCommand, int>
{
    public async Task<int> Handle(CreateFatorComposicaoCombustivelCommand request, CancellationToken cancellationToken)
    {
        var fator = new FatorComposicaoCombustivel
        {
            Mes = request.Mes,
            Ano = request.Ano,
            PercentualEtanol = request.PercentualEtanol,
            PercentualBiodiesel = request.PercentualBiodiesel,
        };

        dbContext.FatoresComposicaoCombustivel.Add(fator);
        await dbContext.SaveChangesAsync(cancellationToken);

        return fator.Codigo;
    }
}
