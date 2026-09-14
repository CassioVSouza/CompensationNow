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
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
        RuleFor(x => x.CodigoFrota).GreaterThan(0).WithMessage("Frota é obrigatória.");
        RuleFor(x => x.CodigoCombustivel).GreaterThan(0).When(x => x.CodigoCombustivel is not null)
            .WithMessage("Combustível inválido.");
        RuleFor(x => x.DataReferencia).NotEqual(default(DateOnly)).WithMessage("Data de referência é obrigatória.");
        RuleFor(x => x.Consumo).GreaterThanOrEqualTo(0).WithMessage("Consumo deve ser maior ou igual a zero.");
        RuleFor(x => x.AnoFrota).GreaterThanOrEqualTo(0).WithMessage("Ano da frota deve ser maior ou igual a zero.");
        RuleFor(x => x.DistanciaKM).GreaterThanOrEqualTo(0).WithMessage("Distância (KM) deve ser maior ou igual a zero.");
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
