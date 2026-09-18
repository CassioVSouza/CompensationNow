using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.ConsumosMedioFrota.Commands.UpdateConsumoMedioFrota;

public record UpdateConsumoMedioFrotaCommand(
    int Codigo,
    int CodigoFrota,
    bool ParaTodos,
    int Ano,
    decimal Consumo,
    string UnidadeMedida) : IRequest;

public class UpdateConsumoMedioFrotaCommandValidator : AbstractValidator<UpdateConsumoMedioFrotaCommand>
{
    public UpdateConsumoMedioFrotaCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
        RuleFor(x => x.CodigoFrota).GreaterThan(0).WithMessage("Frota é obrigatória.");
        RuleFor(x => x.Ano).GreaterThan(1900).WithMessage("Ano inválido.");
        RuleFor(x => x.Consumo).GreaterThan(0).WithMessage("Consumo deve ser maior que zero.");
        RuleFor(x => x.UnidadeMedida).NotEmpty().WithMessage("Unidade de medida é obrigatória.")
            .MaximumLength(20).WithMessage("Unidade de medida deve ter no máximo 20 caracteres.");
    }
}

public class UpdateConsumoMedioFrotaCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<UpdateConsumoMedioFrotaCommand>
{
    public async Task Handle(UpdateConsumoMedioFrotaCommand request, CancellationToken cancellationToken)
    {
        var consumo = await dbContext.ConsumosMedioFrota.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(ConsumoMedioFrota), request.Codigo);

        consumo.CodigoFrota = request.CodigoFrota;
        consumo.ParaTodos = request.ParaTodos;
        consumo.Ano = request.Ano;
        consumo.Consumo = request.Consumo;
        consumo.UnidadeMedida = request.UnidadeMedida;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
