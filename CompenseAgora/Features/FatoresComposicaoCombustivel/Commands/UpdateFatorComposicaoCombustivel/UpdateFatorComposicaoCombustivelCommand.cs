using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.FatoresComposicaoCombustivel.Commands.UpdateFatorComposicaoCombustivel;

public record UpdateFatorComposicaoCombustivelCommand(
    int Codigo,
    int Mes,
    int Ano,
    decimal PercentualEtanol,
    decimal PercentualBiodiesel) : IRequest;

public class UpdateFatorComposicaoCombustivelCommandValidator : AbstractValidator<UpdateFatorComposicaoCombustivelCommand>
{
    public UpdateFatorComposicaoCombustivelCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
        RuleFor(x => x.Mes).InclusiveBetween(1, 12).WithMessage("Mês deve estar entre 1 e 12.");
        RuleFor(x => x.Ano).GreaterThan(1900).WithMessage("Ano inválido.");
        RuleFor(x => x.PercentualEtanol).InclusiveBetween(0, 100).WithMessage("Percentual de etanol deve estar entre 0 e 100.");
        RuleFor(x => x.PercentualBiodiesel).InclusiveBetween(0, 100).WithMessage("Percentual de biodiesel deve estar entre 0 e 100.");
    }
}

public class UpdateFatorComposicaoCombustivelCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<UpdateFatorComposicaoCombustivelCommand>
{
    public async Task Handle(UpdateFatorComposicaoCombustivelCommand request, CancellationToken cancellationToken)
    {
        var fator = await dbContext.FatoresComposicaoCombustivel.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(FatorComposicaoCombustivel), request.Codigo);

        fator.Mes = request.Mes;
        fator.Ano = request.Ano;
        fator.PercentualEtanol = request.PercentualEtanol;
        fator.PercentualBiodiesel = request.PercentualBiodiesel;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
