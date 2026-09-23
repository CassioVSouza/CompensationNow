using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Auditorias.Commands.RegistrarFiltro;

/// <summary>
/// Records that a user applied a period filter on a screen. CRUD actions are audited automatically by
/// <c>AuditoriaInterceptor</c>; filters don't touch the database, so screens send this explicitly.
/// </summary>
public record RegistrarFiltroCommand(int CodigoPessoa, string Tela, DateOnly DataInicio, DateOnly DataFim) : IRequest;

public class RegistrarFiltroCommandValidator : AbstractValidator<RegistrarFiltroCommand>
{
    public RegistrarFiltroCommandValidator()
    {
        RuleFor(x => x.CodigoPessoa).GreaterThan(0).WithMessage("Pessoa é obrigatória.");
        RuleFor(x => x.Tela).NotEmpty().WithMessage("Tela é obrigatória.")
            .MaximumLength(50).WithMessage("Tela deve ter no máximo 50 caracteres.");
        RuleFor(x => x.DataFim).GreaterThanOrEqualTo(x => x.DataInicio)
            .WithMessage("A data de início não pode ser posterior à data de fim.");
    }
}

public class RegistrarFiltroCommandHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<RegistrarFiltroCommand>
{
    public async Task Handle(RegistrarFiltroCommand request, CancellationToken cancellationToken)
    {
        dbContext.Auditorias.Add(new Auditoria
        {
            CodigoPessoa = request.CodigoPessoa,
            DataHora = DateTime.UtcNow,
            Acao = TipoAcaoAuditoria.Filtro,
            Tela = request.Tela,
            DataInicioFiltro = request.DataInicio,
            DataFimFiltro = request.DataFim,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
