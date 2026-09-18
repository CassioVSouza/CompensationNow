using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.FatoresEnergia.Commands.DeleteFatorEnergia;

public record DeleteFatorEnergiaCommand(int Codigo) : IRequest;

public class DeleteFatorEnergiaCommandValidator : AbstractValidator<DeleteFatorEnergiaCommand>
{
    public DeleteFatorEnergiaCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
    }
}

public class DeleteFatorEnergiaCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<DeleteFatorEnergiaCommand>
{
    public async Task Handle(DeleteFatorEnergiaCommand request, CancellationToken cancellationToken)
    {
        var fator = await dbContext.FatoresEnergia.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(FatorEnergia), request.Codigo);

        dbContext.FatoresEnergia.Remove(fator);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
