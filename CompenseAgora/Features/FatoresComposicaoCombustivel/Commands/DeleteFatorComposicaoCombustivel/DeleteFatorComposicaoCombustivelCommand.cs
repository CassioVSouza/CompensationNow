using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.FatoresComposicaoCombustivel.Commands.DeleteFatorComposicaoCombustivel;

public record DeleteFatorComposicaoCombustivelCommand(int Codigo) : IRequest;

public class DeleteFatorComposicaoCombustivelCommandValidator : AbstractValidator<DeleteFatorComposicaoCombustivelCommand>
{
    public DeleteFatorComposicaoCombustivelCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
    }
}

public class DeleteFatorComposicaoCombustivelCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<DeleteFatorComposicaoCombustivelCommand>
{
    public async Task Handle(DeleteFatorComposicaoCombustivelCommand request, CancellationToken cancellationToken)
    {
        var fator = await dbContext.FatoresComposicaoCombustivel.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(FatorComposicaoCombustivel), request.Codigo);

        dbContext.FatoresComposicaoCombustivel.Remove(fator);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
