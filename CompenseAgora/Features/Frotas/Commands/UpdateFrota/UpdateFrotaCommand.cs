using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Frotas.Commands.UpdateFrota;

public record UpdateFrotaCommand(
    int Codigo,
    string Nome,
    int CodigoCombustivelPrimario,
    int? CodigoCombustivelBiogenico,
    int? CodigoCombustivelFossil) : IRequest;

public class UpdateFrotaCommandValidator : AbstractValidator<UpdateFrotaCommand>
{
    public UpdateFrotaCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("Nome deve ter no máximo 150 caracteres.");
        RuleFor(x => x.CodigoCombustivelPrimario).GreaterThan(0).WithMessage("Combustível primário é obrigatório.");
        RuleFor(x => x.CodigoCombustivelBiogenico).GreaterThan(0).When(x => x.CodigoCombustivelBiogenico is not null)
            .WithMessage("Combustível biogênico inválido.");
        RuleFor(x => x.CodigoCombustivelFossil).GreaterThan(0).When(x => x.CodigoCombustivelFossil is not null)
            .WithMessage("Combustível fóssil inválido.");
    }
}

public class UpdateFrotaCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<UpdateFrotaCommand>
{
    public async Task Handle(UpdateFrotaCommand request, CancellationToken cancellationToken)
    {
        var frota = await dbContext.Frotas.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(Frota), request.Codigo);

        frota.Nome = request.Nome;
        frota.CodigoCombustivelPrimario = request.CodigoCombustivelPrimario;
        frota.CodigoCombustivelBiogenico = request.CodigoCombustivelBiogenico;
        frota.CodigoCombustivelFossil = request.CodigoCombustivelFossil;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
