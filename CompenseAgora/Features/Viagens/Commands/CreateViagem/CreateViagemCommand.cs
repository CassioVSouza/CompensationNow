using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Viagens.Commands.CreateViagem;

public record CreateViagemCommand(
    int CodigoPessoa,
    int CodigoFrota,
    int? CodigoCombustivel,
    DateOnly DataReferencia,
    decimal Consumo,
    int AnoFrota,
    decimal DistanciaKM) : IRequest<int>;

public class CreateViagemCommandValidator : AbstractValidator<CreateViagemCommand>
{
    public CreateViagemCommandValidator()
    {
        RuleFor(x => x.CodigoPessoa).GreaterThan(0);
        RuleFor(x => x.CodigoFrota).GreaterThan(0);
        RuleFor(x => x.CodigoCombustivel).GreaterThan(0).When(x => x.CodigoCombustivel is not null);
        RuleFor(x => x.DataReferencia).NotEqual(default(DateOnly));
        RuleFor(x => x.Consumo).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AnoFrota).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DistanciaKM).GreaterThanOrEqualTo(0);
    }
}

public class CreateViagemCommandHandler(CompenseAgoraDbContext dbContext) : IRequestHandler<CreateViagemCommand, int>
{
    public async Task<int> Handle(CreateViagemCommand request, CancellationToken cancellationToken)
    {
        var viagem = new Viagem
        {
            CodigoPessoa = request.CodigoPessoa,
            CodigoFrota = request.CodigoFrota,
            CodigoCombustivel = request.CodigoCombustivel,
            CriadoEm = DateOnly.FromDateTime(DateTime.UtcNow),
            DataReferencia = request.DataReferencia,
            Consumo = request.Consumo,
            AnoFrota = request.AnoFrota,
            DistanciaKM = request.DistanciaKM,
            EmissaoCO2 = 0,
        };

        dbContext.Viagens.Add(viagem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return viagem.Codigo;
    }
}
