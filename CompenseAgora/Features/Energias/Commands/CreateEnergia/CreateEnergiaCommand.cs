using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Energias.Commands.CreateEnergia;

public record CreateEnergiaCommand(
    int CodigoPessoa,
    DateOnly DataReferencia,
    decimal Quantidade) : IRequest<int>;

public class CreateEnergiaCommandValidator : AbstractValidator<CreateEnergiaCommand>
{
    public CreateEnergiaCommandValidator()
    {
        RuleFor(x => x.CodigoPessoa).GreaterThan(0);
        RuleFor(x => x.DataReferencia).NotEqual(default(DateOnly));
        RuleFor(x => x.Quantidade).GreaterThan(0);
    }
}

public class CreateEnergiaCommandHandler(CompenseAgoraDbContext dbContext) : IRequestHandler<CreateEnergiaCommand, int>
{
    public async Task<int> Handle(CreateEnergiaCommand request, CancellationToken cancellationToken)
    {
        var energia = new Energia
        {
            CodigoPessoa = request.CodigoPessoa,
            CriadoEm = DateOnly.FromDateTime(DateTime.UtcNow),
            DataReferencia = request.DataReferencia,
            Quantidade = request.Quantidade,
            EmissaoCO2 = 0,
        };

        dbContext.Energias.Add(energia);
        await dbContext.SaveChangesAsync(cancellationToken);

        return energia.Codigo;
    }
}
