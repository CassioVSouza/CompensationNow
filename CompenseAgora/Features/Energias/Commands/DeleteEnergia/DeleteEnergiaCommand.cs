using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Energias.Commands.DeleteEnergia;

public record DeleteEnergiaCommand(int Codigo) : IRequest;

public class DeleteEnergiaCommandValidator : AbstractValidator<DeleteEnergiaCommand>
{
    public DeleteEnergiaCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
    }
}

public class DeleteEnergiaCommandHandler(CompenseAgoraDbContext dbContext) : IRequestHandler<DeleteEnergiaCommand>
{
    public async Task Handle(DeleteEnergiaCommand request, CancellationToken cancellationToken)
    {
        var energia = await dbContext.Energias.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(Energia), request.Codigo);

        dbContext.Energias.Remove(energia);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
