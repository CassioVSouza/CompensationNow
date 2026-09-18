using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Compensacoes.Commands.DeleteCompensacao;

public record DeleteCompensacaoCommand(int Codigo) : IRequest;

public class DeleteCompensacaoCommandValidator : AbstractValidator<DeleteCompensacaoCommand>
{
    public DeleteCompensacaoCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
    }
}

public class DeleteCompensacaoCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<DeleteCompensacaoCommand>
{
    public async Task Handle(DeleteCompensacaoCommand request, CancellationToken cancellationToken)
    {
        var compensacao = await dbContext.Compensacoes.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(Compensacao), request.Codigo);

        dbContext.Compensacoes.Remove(compensacao);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
