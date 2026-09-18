using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.ConsumosMedioFrota.Commands.CreateConsumoMedioFrota;

public record CreateConsumoMedioFrotaCommand(
    int CodigoFrota,
    bool ParaTodos,
    int Ano,
    decimal Consumo,
    string UnidadeMedida) : IRequest<int>;

public class CreateConsumoMedioFrotaCommandValidator : AbstractValidator<CreateConsumoMedioFrotaCommand>
{
    public CreateConsumoMedioFrotaCommandValidator()
    {
        RuleFor(x => x.CodigoFrota).GreaterThan(0).WithMessage("Frota é obrigatória.");
        RuleFor(x => x.Ano).GreaterThan(1900).WithMessage("Ano inválido.");
        RuleFor(x => x.Consumo).GreaterThan(0).WithMessage("Consumo deve ser maior que zero.");
        RuleFor(x => x.UnidadeMedida).NotEmpty().WithMessage("Unidade de medida é obrigatória.")
            .MaximumLength(20).WithMessage("Unidade de medida deve ter no máximo 20 caracteres.");
    }
}

public class CreateConsumoMedioFrotaCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<CreateConsumoMedioFrotaCommand, int>
{
    public async Task<int> Handle(CreateConsumoMedioFrotaCommand request, CancellationToken cancellationToken)
    {
        var consumo = new ConsumoMedioFrota
        {
            CodigoFrota = request.CodigoFrota,
            ParaTodos = request.ParaTodos,
            Ano = request.Ano,
            Consumo = request.Consumo,
            UnidadeMedida = request.UnidadeMedida,
        };

        dbContext.ConsumosMedioFrota.Add(consumo);
        await dbContext.SaveChangesAsync(cancellationToken);

        return consumo.Codigo;
    }
}
