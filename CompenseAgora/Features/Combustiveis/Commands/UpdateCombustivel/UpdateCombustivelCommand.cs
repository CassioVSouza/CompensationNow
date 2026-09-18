using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Combustiveis.Commands.UpdateCombustivel;

public record UpdateCombustivelCommand(
    int Codigo,
    string Nome,
    string UnidadeMedida,
    bool CombustivelPrincipal,
    int? CodigoCombustivelBiogenico,
    int? CodigoCombustivelFossil) : IRequest;

public class UpdateCombustivelCommandValidator : AbstractValidator<UpdateCombustivelCommand>
{
    public UpdateCombustivelCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("Nome deve ter no máximo 150 caracteres.");
        RuleFor(x => x.UnidadeMedida).NotEmpty().WithMessage("Unidade de medida é obrigatória.")
            .MaximumLength(20).WithMessage("Unidade de medida deve ter no máximo 20 caracteres.");
        RuleFor(x => x.CodigoCombustivelBiogenico).GreaterThan(0).When(x => x.CodigoCombustivelBiogenico is not null)
            .WithMessage("Combustível biogênico inválido.");
        RuleFor(x => x.CodigoCombustivelBiogenico).NotEqual(x => x.Codigo).When(x => x.CodigoCombustivelBiogenico is not null)
            .WithMessage("Um combustível não pode referenciar a si mesmo.");
        RuleFor(x => x.CodigoCombustivelFossil).GreaterThan(0).When(x => x.CodigoCombustivelFossil is not null)
            .WithMessage("Combustível fóssil inválido.");
        RuleFor(x => x.CodigoCombustivelFossil).NotEqual(x => x.Codigo).When(x => x.CodigoCombustivelFossil is not null)
            .WithMessage("Um combustível não pode referenciar a si mesmo.");
    }
}

public class UpdateCombustivelCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<UpdateCombustivelCommand>
{
    public async Task Handle(UpdateCombustivelCommand request, CancellationToken cancellationToken)
    {
        var combustivel = await dbContext.Combustiveis.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(Combustivel), request.Codigo);

        combustivel.Nome = request.Nome;
        combustivel.UnidadeMedida = request.UnidadeMedida;
        combustivel.CombustivelPrincipal = request.CombustivelPrincipal;
        combustivel.CodigoCombustivelBiogenico = request.CodigoCombustivelBiogenico;
        combustivel.CodigoCombustivelFossil = request.CodigoCombustivelFossil;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
