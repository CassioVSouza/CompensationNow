using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.GasesEfeitoEstufa.Commands.UpdateGasEfeitoEstufa;

public record UpdateGasEfeitoEstufaCommand(int Codigo, string Nome, string Familia, decimal GWP) : IRequest;

public class UpdateGasEfeitoEstufaCommandValidator : AbstractValidator<UpdateGasEfeitoEstufaCommand>
{
    public UpdateGasEfeitoEstufaCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("Nome deve ter no máximo 150 caracteres.");
        RuleFor(x => x.Familia).NotEmpty().WithMessage("Família é obrigatória.")
            .MaximumLength(150).WithMessage("Família deve ter no máximo 150 caracteres.");
        RuleFor(x => x.GWP).GreaterThan(0).WithMessage("GWP deve ser maior que zero.");
    }
}

public class UpdateGasEfeitoEstufaCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<UpdateGasEfeitoEstufaCommand>
{
    public async Task Handle(UpdateGasEfeitoEstufaCommand request, CancellationToken cancellationToken)
    {
        var gas = await dbContext.GasesEfeitoEstufa.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(GasEfeitoEstufa), request.Codigo);

        gas.Nome = request.Nome;
        gas.Familia = request.Familia;
        gas.GWP = request.GWP;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
