using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.ConsumosMedioFrota.Commands.DeleteConsumoMedioFrota;

public record DeleteConsumoMedioFrotaCommand(int Codigo) : IRequest;

public class DeleteConsumoMedioFrotaCommandValidator : AbstractValidator<DeleteConsumoMedioFrotaCommand>
{
    public DeleteConsumoMedioFrotaCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
    }
}

public class DeleteConsumoMedioFrotaCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<DeleteConsumoMedioFrotaCommand>
{
    public async Task Handle(DeleteConsumoMedioFrotaCommand request, CancellationToken cancellationToken)
    {
        var consumo = await dbContext.ConsumosMedioFrota.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(ConsumoMedioFrota), request.Codigo);

        dbContext.ConsumosMedioFrota.Remove(consumo);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
