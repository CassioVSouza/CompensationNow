using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Compensacoes.Commands.UpdateCompensacao;

public record UpdateCompensacaoCommand(
    int Codigo,
    DateOnly DataReferencia,
    string TipoCompensacao,
    decimal QuantidadeCompensada) : IRequest;

public class UpdateCompensacaoCommandValidator : AbstractValidator<UpdateCompensacaoCommand>
{
    public UpdateCompensacaoCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
        RuleFor(x => x.DataReferencia).NotEqual(default(DateOnly)).WithMessage("Data de referência é obrigatória.");
        RuleFor(x => x.TipoCompensacao).NotEmpty().WithMessage("Tipo de compensação é obrigatório.")
            .MaximumLength(20).WithMessage("Tipo de compensação deve ter no máximo 20 caracteres.");
        RuleFor(x => x.QuantidadeCompensada).GreaterThan(0).WithMessage("Quantidade compensada deve ser maior que zero.");
    }
}

public class UpdateCompensacaoCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<UpdateCompensacaoCommand>
{
    public async Task Handle(UpdateCompensacaoCommand request, CancellationToken cancellationToken)
    {
        var compensacao = await dbContext.Compensacoes.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(Compensacao), request.Codigo);

        compensacao.DataReferencia = request.DataReferencia;
        compensacao.TipoCompensacao = request.TipoCompensacao;
        compensacao.QuantidadeCompensada = request.QuantidadeCompensada;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
