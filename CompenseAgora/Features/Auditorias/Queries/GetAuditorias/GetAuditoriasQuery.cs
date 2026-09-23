using System.Text.Json;
using CompenseAgora.Common;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Auditorias.Queries.GetAuditorias;

/// <summary>
/// A page of the user's own audit trail, newest first. <see cref="DataInicio"/>/<see cref="DataFim"/> are
/// Brasília calendar days (inclusive); <see cref="Pagina"/> is zero-based.
/// </summary>
public record GetAuditoriasQuery(
    int CodigoPessoa,
    TipoAcaoAuditoria? Acao = null,
    DateOnly? DataInicio = null,
    DateOnly? DataFim = null,
    int Pagina = 0,
    int TamanhoPagina = 20) : IRequest<AuditoriasPaginadasDto>;

public class GetAuditoriasQueryValidator : AbstractValidator<GetAuditoriasQuery>
{
    public GetAuditoriasQueryValidator()
    {
        RuleFor(x => x.CodigoPessoa).GreaterThan(0).WithMessage("Pessoa é obrigatória.");
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(0).WithMessage("Página inválida.");
        RuleFor(x => x.TamanhoPagina).InclusiveBetween(1, 100).WithMessage("Tamanho de página deve estar entre 1 e 100.");
        RuleFor(x => x.DataFim).GreaterThanOrEqualTo(x => x.DataInicio)
            .When(x => x.DataInicio is not null && x.DataFim is not null)
            .WithMessage("A data de início não pode ser posterior à data de fim.");
    }
}

public class GetAuditoriasQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetAuditoriasQuery, AuditoriasPaginadasDto>
{
    public async Task<AuditoriasPaginadasDto> Handle(GetAuditoriasQuery request, CancellationToken cancellationToken)
    {
        var consulta = dbContext.Auditorias
            .AsNoTracking()
            .Where(a => a.CodigoPessoa == request.CodigoPessoa);

        if (request.Acao is { } acao)
        {
            consulta = consulta.Where(a => a.Acao == acao);
        }

        if (request.DataInicio is not null)
        {
            var inicioUtc = FusoHorarioBrasil.InicioDoDiaEmUtc(request.DataInicio.Value);
            consulta = consulta.Where(a => a.DataHora >= inicioUtc);
        }

        if (request.DataFim is not null)
        {
            var fimExclusivoUtc = FusoHorarioBrasil.InicioDoDiaEmUtc(request.DataFim.Value.AddDays(1));
            consulta = consulta.Where(a => a.DataHora < fimExclusivoUtc);
        }

        var total = await consulta.CountAsync(cancellationToken);

        var linhas = await consulta
            .OrderByDescending(a => a.DataHora)
            .ThenByDescending(a => a.Codigo)
            .Skip(request.Pagina * request.TamanhoPagina)
            .Take(request.TamanhoPagina)
            .Select(a => new
            {
                a.Codigo,
                a.DataHora,
                a.Acao,
                a.Entidade,
                a.CodigoRegistro,
                a.Tela,
                a.DataInicioFiltro,
                a.DataFimFiltro,
                a.Detalhes,
            })
            .ToListAsync(cancellationToken);

        // Labels and JSON details are resolved in memory: they aren't translatable to SQL.
        var itens = linhas
            .Select(a => new AuditoriaDto(
                a.Codigo,
                a.DataHora,
                a.Acao,
                RotulosAuditoria.Descrever(a.Acao, a.Entidade, a.CodigoRegistro, a.Tela, a.DataInicioFiltro, a.DataFimFiltro),
                LerAlteracoes(a.Detalhes)))
            .ToList();

        return new AuditoriasPaginadasDto(itens, total);
    }

    private static IReadOnlyList<AlteracaoCampo> LerAlteracoes(string? detalhes) =>
        string.IsNullOrEmpty(detalhes)
            ? []
            : (JsonSerializer.Deserialize<List<AlteracaoCampo>>(detalhes) ?? [])
                .Select(c => c with { Campo = RotulosAuditoria.Campo(c.Campo) })
                .ToList();
}
