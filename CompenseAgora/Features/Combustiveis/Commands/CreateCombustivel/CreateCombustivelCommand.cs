using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Combustiveis.Commands.CreateCombustivel;

public record CreateCombustivelCommand(
    string Nome,
    string UnidadeMedida,
    bool CombustivelPrincipal,
    int? CodigoCombustivelBiogenico,
    int? CodigoCombustivelFossil) : IRequest<int>;

public class CreateCombustivelCommandValidator : AbstractValidator<CreateCombustivelCommand>
{
    public CreateCombustivelCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("Nome deve ter no máximo 150 caracteres.");
        RuleFor(x => x.UnidadeMedida).NotEmpty().WithMessage("Unidade de medida é obrigatória.")
            .MaximumLength(20).WithMessage("Unidade de medida deve ter no máximo 20 caracteres.");
        RuleFor(x => x.CodigoCombustivelBiogenico).GreaterThan(0).When(x => x.CodigoCombustivelBiogenico is not null)
            .WithMessage("Combustível biogênico inválido.");
        RuleFor(x => x.CodigoCombustivelFossil).GreaterThan(0).When(x => x.CodigoCombustivelFossil is not null)
            .WithMessage("Combustível fóssil inválido.");
    }
}

public class CreateCombustivelCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<CreateCombustivelCommand, int>
{
    public async Task<int> Handle(CreateCombustivelCommand request, CancellationToken cancellationToken)
    {
        var combustivel = new Combustivel
        {
            Nome = request.Nome,
            UnidadeMedida = request.UnidadeMedida,
            CombustivelPrincipal = request.CombustivelPrincipal,
            CodigoCombustivelBiogenico = request.CodigoCombustivelBiogenico,
            CodigoCombustivelFossil = request.CodigoCombustivelFossil,
        };

        dbContext.Combustiveis.Add(combustivel);
        await dbContext.SaveChangesAsync(cancellationToken);

        return combustivel.Codigo;
    }
}
