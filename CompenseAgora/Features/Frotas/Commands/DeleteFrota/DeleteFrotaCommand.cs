using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Frotas.Commands.DeleteFrota;

public record DeleteFrotaCommand(int Codigo) : IRequest;

public class DeleteFrotaCommandValidator : AbstractValidator<DeleteFrotaCommand>
{
    public DeleteFrotaCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
    }
}

public class DeleteFrotaCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<DeleteFrotaCommand>
{
    public async Task Handle(DeleteFrotaCommand request, CancellationToken cancellationToken)
    {
        var frota = await dbContext.Frotas.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(Frota), request.Codigo);

        dbContext.Frotas.Remove(frota);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
