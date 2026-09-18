using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Frotas.Commands.CreateFrota;

public record CreateFrotaCommand(
    string Nome,
    int CodigoCombustivelPrimario,
    int? CodigoCombustivelBiogenico,
    int? CodigoCombustivelFossil) : IRequest<int>;

public class CreateFrotaCommandValidator : AbstractValidator<CreateFrotaCommand>
{
    public CreateFrotaCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("Nome deve ter no máximo 150 caracteres.");
        RuleFor(x => x.CodigoCombustivelPrimario).GreaterThan(0).WithMessage("Combustível primário é obrigatório.");
        RuleFor(x => x.CodigoCombustivelBiogenico).GreaterThan(0).When(x => x.CodigoCombustivelBiogenico is not null)
            .WithMessage("Combustível biogênico inválido.");
        RuleFor(x => x.CodigoCombustivelFossil).GreaterThan(0).When(x => x.CodigoCombustivelFossil is not null)
            .WithMessage("Combustível fóssil inválido.");
    }
}

public class CreateFrotaCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<CreateFrotaCommand, int>
{
    public async Task<int> Handle(CreateFrotaCommand request, CancellationToken cancellationToken)
    {
        var frota = new Frota
        {
            Nome = request.Nome,
            CodigoCombustivelPrimario = request.CodigoCombustivelPrimario,
            CodigoCombustivelBiogenico = request.CodigoCombustivelBiogenico,
            CodigoCombustivelFossil = request.CodigoCombustivelFossil,
        };

        dbContext.Frotas.Add(frota);
        await dbContext.SaveChangesAsync(cancellationToken);

        return frota.Codigo;
    }
}
