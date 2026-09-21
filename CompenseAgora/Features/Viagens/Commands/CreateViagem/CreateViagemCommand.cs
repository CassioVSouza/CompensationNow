using CompenseAgora.Data;
using CompenseAgora.Entities;
using CompenseAgora.Features.Viagens.Calculo;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Viagens.Commands.CreateViagem;

public record CreateViagemCommand(
    int CodigoPessoa,
    int? CodigoFrota,
    int? CodigoCombustivel,
    DateOnly DataReferencia,
    decimal Consumo,
    int AnoFrota,
    decimal DistanciaKM) : IRequest<int>;

public class CreateViagemCommandValidator : AbstractValidator<CreateViagemCommand>
{
    public CreateViagemCommandValidator()
    {
        RuleFor(x => x.CodigoPessoa).GreaterThan(0).WithMessage("Pessoa é obrigatória.");
        RuleFor(x => x.CodigoFrota).GreaterThan(0).When(x => x.CodigoFrota is not null)
            .WithMessage("Veículo inválido.");
        RuleFor(x => x.CodigoCombustivel).GreaterThan(0).When(x => x.CodigoCombustivel is not null)
            .WithMessage("Combustível inválido.");
        RuleFor(x => x)
            .Must(x => x.CodigoFrota is not null || x.CodigoCombustivel is not null)
            .WithMessage("Informe um veículo ou um combustível.")
            .WithName("CodigoFrota");
        RuleFor(x => x.DataReferencia).NotEqual(default(DateOnly)).WithMessage("Data de referência é obrigatória.");
        RuleFor(x => x.Consumo).GreaterThanOrEqualTo(0).WithMessage("Consumo deve ser maior ou igual a zero.");
        RuleFor(x => x.AnoFrota).GreaterThanOrEqualTo(0).WithMessage("Ano do veículo deve ser maior ou igual a zero.");
        RuleFor(x => x.DistanciaKM).GreaterThanOrEqualTo(0).WithMessage("Distância (KM) deve ser maior ou igual a zero.");
    }
}

public class CreateViagemCommandHandler(CompenseAgoraDbContext dbContext, ICalculadoraEmissaoViagem calculadoraEmissao)
    : IRequestHandler<CreateViagemCommand, int>
{
    public async Task<int> Handle(CreateViagemCommand request, CancellationToken cancellationToken)
    {
        var emissaoCO2 = await calculadoraEmissao.CalcularEmissaoCO2Async(
            request.CodigoFrota, request.CodigoCombustivel, request.DataReferencia, request.Consumo, request.AnoFrota, request.DistanciaKM, cancellationToken);

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
            EmissaoCO2 = emissaoCO2,
        };

        dbContext.Viagens.Add(viagem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return viagem.Codigo;
    }
}
