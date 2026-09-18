using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using CompenseAgora.Features.Energias.Calculo;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Energias.Commands.UpdateEnergia;

public record UpdateEnergiaCommand(
    int Codigo,
    DateOnly DataReferencia,
    decimal Quantidade) : IRequest;

public class UpdateEnergiaCommandValidator : AbstractValidator<UpdateEnergiaCommand>
{
    public UpdateEnergiaCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
        RuleFor(x => x.DataReferencia).NotEqual(default(DateOnly)).WithMessage("Data de referência é obrigatória.");
        RuleFor(x => x.Quantidade).GreaterThan(0).WithMessage("Quantidade deve ser maior que zero.");
    }
}

public class UpdateEnergiaCommandHandler(CompenseAgoraDbContext dbContext, ICalculadoraEmissaoEnergia calculadoraEmissao)
    : IRequestHandler<UpdateEnergiaCommand>
{
    public async Task Handle(UpdateEnergiaCommand request, CancellationToken cancellationToken)
    {
        var energia = await dbContext.Energias.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(Energia), request.Codigo);

        energia.DataReferencia = request.DataReferencia;
        energia.Quantidade = request.Quantidade;
        energia.EmissaoCO2 = await calculadoraEmissao.CalcularEmissaoCO2Async(
            request.DataReferencia, request.Quantidade, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
