using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Viagens.Commands.UpdateViagem;

public record UpdateViagemCommand(
    int Codigo,
    int CodigoFrota,
    int? CodigoCombustivel,
    DateOnly DataReferencia,
    decimal Consumo,
    int AnoFrota,
    decimal DistanciaKM) : IRequest;

public class UpdateViagemCommandValidator : AbstractValidator<UpdateViagemCommand>
{
    public UpdateViagemCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0);
        RuleFor(x => x.CodigoFrota).GreaterThan(0);
        RuleFor(x => x.CodigoCombustivel).GreaterThan(0).When(x => x.CodigoCombustivel is not null);
        RuleFor(x => x.DataReferencia).NotEqual(default(DateOnly));
        RuleFor(x => x.Consumo).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AnoFrota).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DistanciaKM).GreaterThanOrEqualTo(0);
    }
}

public class UpdateViagemCommandHandler(CompenseAgoraDbContext dbContext) : IRequestHandler<UpdateViagemCommand>
{
    public async Task Handle(UpdateViagemCommand request, CancellationToken cancellationToken)
    {
        var viagem = await dbContext.Viagens.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(Viagem), request.Codigo);

        viagem.CodigoFrota = request.CodigoFrota;
        viagem.CodigoCombustivel = request.CodigoCombustivel;
        viagem.DataReferencia = request.DataReferencia;
        viagem.Consumo = request.Consumo;
        viagem.AnoFrota = request.AnoFrota;
        viagem.DistanciaKM = request.DistanciaKM;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
