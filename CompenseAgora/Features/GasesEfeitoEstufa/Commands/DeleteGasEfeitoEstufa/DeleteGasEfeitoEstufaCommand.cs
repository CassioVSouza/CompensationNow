using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.GasesEfeitoEstufa.Commands.DeleteGasEfeitoEstufa;

public record DeleteGasEfeitoEstufaCommand(int Codigo) : IRequest;

public class DeleteGasEfeitoEstufaCommandValidator : AbstractValidator<DeleteGasEfeitoEstufaCommand>
{
    public DeleteGasEfeitoEstufaCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
    }
}

public class DeleteGasEfeitoEstufaCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<DeleteGasEfeitoEstufaCommand>
{
    public async Task Handle(DeleteGasEfeitoEstufaCommand request, CancellationToken cancellationToken)
    {
        var gas = await dbContext.GasesEfeitoEstufa.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(GasEfeitoEstufa), request.Codigo);

        dbContext.GasesEfeitoEstufa.Remove(gas);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
