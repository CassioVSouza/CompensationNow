using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.FatoresEnergia.Commands.CreateFatorEnergia;

public record CreateFatorEnergiaCommand(int Mes, int Ano, decimal FeSin) : IRequest<int>;

public class CreateFatorEnergiaCommandValidator : AbstractValidator<CreateFatorEnergiaCommand>
{
    public CreateFatorEnergiaCommandValidator()
    {
        RuleFor(x => x.Mes).InclusiveBetween(1, 12).WithMessage("Mês deve estar entre 1 e 12.");
        RuleFor(x => x.Ano).GreaterThan(1900).WithMessage("Ano inválido.");
        RuleFor(x => x.FeSin).GreaterThanOrEqualTo(0).WithMessage("FeSin deve ser maior ou igual a zero.");
    }
}

public class CreateFatorEnergiaCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<CreateFatorEnergiaCommand, int>
{
    public async Task<int> Handle(CreateFatorEnergiaCommand request, CancellationToken cancellationToken)
    {
        var fator = new FatorEnergia
        {
            Mes = request.Mes,
            Ano = request.Ano,
            FeSin = request.FeSin,
        };

        dbContext.FatoresEnergia.Add(fator);
        await dbContext.SaveChangesAsync(cancellationToken);

        return fator.Codigo;
    }
}
