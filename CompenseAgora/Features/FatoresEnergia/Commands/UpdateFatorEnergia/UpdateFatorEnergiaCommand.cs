using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.FatoresEnergia.Commands.UpdateFatorEnergia;

public record UpdateFatorEnergiaCommand(int Codigo, int Mes, int Ano, decimal FeSin) : IRequest;

public class UpdateFatorEnergiaCommandValidator : AbstractValidator<UpdateFatorEnergiaCommand>
{
    public UpdateFatorEnergiaCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
        RuleFor(x => x.Mes).InclusiveBetween(1, 12).WithMessage("Mês deve estar entre 1 e 12.");
        RuleFor(x => x.Ano).GreaterThan(1900).WithMessage("Ano inválido.");
        RuleFor(x => x.FeSin).GreaterThanOrEqualTo(0).WithMessage("FeSin deve ser maior ou igual a zero.");
    }
}

public class UpdateFatorEnergiaCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<UpdateFatorEnergiaCommand>
{
    public async Task Handle(UpdateFatorEnergiaCommand request, CancellationToken cancellationToken)
    {
        var fator = await dbContext.FatoresEnergia.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(FatorEnergia), request.Codigo);

        fator.Mes = request.Mes;
        fator.Ano = request.Ano;
        fator.FeSin = request.FeSin;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
