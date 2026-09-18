using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Combustiveis.Commands.DeleteCombustivel;

public record DeleteCombustivelCommand(int Codigo) : IRequest;

public class DeleteCombustivelCommandValidator : AbstractValidator<DeleteCombustivelCommand>
{
    public DeleteCombustivelCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
    }
}

public class DeleteCombustivelCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<DeleteCombustivelCommand>
{
    public async Task Handle(DeleteCombustivelCommand request, CancellationToken cancellationToken)
    {
        var combustivel = await dbContext.Combustiveis.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(Combustivel), request.Codigo);

        dbContext.Combustiveis.Remove(combustivel);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
