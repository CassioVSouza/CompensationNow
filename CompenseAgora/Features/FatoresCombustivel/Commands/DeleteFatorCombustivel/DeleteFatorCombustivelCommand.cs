using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.FatoresCombustivel.Commands.DeleteFatorCombustivel;

public record DeleteFatorCombustivelCommand(int Codigo) : IRequest;

public class DeleteFatorCombustivelCommandValidator : AbstractValidator<DeleteFatorCombustivelCommand>
{
    public DeleteFatorCombustivelCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
    }
}

public class DeleteFatorCombustivelCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<DeleteFatorCombustivelCommand>
{
    public async Task Handle(DeleteFatorCombustivelCommand request, CancellationToken cancellationToken)
    {
        var fator = await dbContext.FatoresDoCombustivel.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(FatorCombustivel), request.Codigo);

        dbContext.FatoresDoCombustivel.Remove(fator);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
