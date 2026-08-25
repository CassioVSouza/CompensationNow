using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Energias.Commands.UpdateEnergia;

public record UpdateEnergiaCommand(
    int Codigo,
    DateOnly DataReferencia,
    decimal Quantidade) : IRequest;

public class UpdateEnergiaCommandValidator : AbstractValidator<UpdateEnergiaCommand>
{
    public UpdateEnergiaCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0);
        RuleFor(x => x.DataReferencia).NotEqual(default(DateOnly));
        RuleFor(x => x.Quantidade).GreaterThan(0);
    }
}

public class UpdateEnergiaCommandHandler(CompenseAgoraDbContext dbContext) : IRequestHandler<UpdateEnergiaCommand>
{
    public async Task Handle(UpdateEnergiaCommand request, CancellationToken cancellationToken)
    {
        var energia = await dbContext.Energias.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(Energia), request.Codigo);

        energia.DataReferencia = request.DataReferencia;
        energia.Quantidade = request.Quantidade;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
