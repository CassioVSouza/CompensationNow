using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.GasesEfeitoEstufa.Commands.CreateGasEfeitoEstufa;

public record CreateGasEfeitoEstufaCommand(string Nome, string Familia, decimal GWP) : IRequest<int>;

public class CreateGasEfeitoEstufaCommandValidator : AbstractValidator<CreateGasEfeitoEstufaCommand>
{
    public CreateGasEfeitoEstufaCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("Nome deve ter no máximo 150 caracteres.");
        RuleFor(x => x.Familia).NotEmpty().WithMessage("Família é obrigatória.")
            .MaximumLength(150).WithMessage("Família deve ter no máximo 150 caracteres.");
        RuleFor(x => x.GWP).GreaterThan(0).WithMessage("GWP deve ser maior que zero.");
    }
}

public class CreateGasEfeitoEstufaCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<CreateGasEfeitoEstufaCommand, int>
{
    public async Task<int> Handle(CreateGasEfeitoEstufaCommand request, CancellationToken cancellationToken)
    {
        var gas = new GasEfeitoEstufa
        {
            Nome = request.Nome,
            Familia = request.Familia,
            GWP = request.GWP,
        };

        dbContext.GasesEfeitoEstufa.Add(gas);
        await dbContext.SaveChangesAsync(cancellationToken);

        return gas.Codigo;
    }
}
