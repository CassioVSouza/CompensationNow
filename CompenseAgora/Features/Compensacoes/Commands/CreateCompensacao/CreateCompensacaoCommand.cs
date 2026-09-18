using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Compensacoes.Commands.CreateCompensacao;

public record CreateCompensacaoCommand(
    int CodigoPessoa,
    DateOnly DataReferencia,
    string TipoCompensacao,
    decimal QuantidadeCompensada) : IRequest<int>;

public class CreateCompensacaoCommandValidator : AbstractValidator<CreateCompensacaoCommand>
{
    public CreateCompensacaoCommandValidator()
    {
        RuleFor(x => x.CodigoPessoa).GreaterThan(0).WithMessage("Pessoa é obrigatória.");
        RuleFor(x => x.DataReferencia).NotEqual(default(DateOnly)).WithMessage("Data de referência é obrigatória.");
        RuleFor(x => x.TipoCompensacao).NotEmpty().WithMessage("Tipo de compensação é obrigatório.")
            .MaximumLength(20).WithMessage("Tipo de compensação deve ter no máximo 20 caracteres.");
        RuleFor(x => x.QuantidadeCompensada).GreaterThan(0).WithMessage("Quantidade compensada deve ser maior que zero.");
    }
}

public class CreateCompensacaoCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<CreateCompensacaoCommand, int>
{
    public async Task<int> Handle(CreateCompensacaoCommand request, CancellationToken cancellationToken)
    {
        var compensacao = new Compensacao
        {
            CodigoPessoa = request.CodigoPessoa,
            CriadoEm = DateOnly.FromDateTime(DateTime.UtcNow),
            DataReferencia = request.DataReferencia,
            TipoCompensacao = request.TipoCompensacao,
            QuantidadeCompensada = request.QuantidadeCompensada,
        };

        dbContext.Compensacoes.Add(compensacao);
        await dbContext.SaveChangesAsync(cancellationToken);

        return compensacao.Codigo;
    }
}
