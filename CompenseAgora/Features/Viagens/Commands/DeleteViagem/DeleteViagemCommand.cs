using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Viagens.Commands.DeleteViagem;

public record DeleteViagemCommand(int Codigo) : IRequest;

public class DeleteViagemCommandValidator : AbstractValidator<DeleteViagemCommand>
{
    public DeleteViagemCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
    }
}

public class DeleteViagemCommandHandler(CompenseAgoraDbContext dbContext) : IRequestHandler<DeleteViagemCommand>
{
    public async Task Handle(DeleteViagemCommand request, CancellationToken cancellationToken)
    {
        var viagem = await dbContext.Viagens.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(Viagem), request.Codigo);

        dbContext.Viagens.Remove(viagem);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
