using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Combustiveis.Queries.GetTodosCombustiveis;

/// <summary>
/// Lista todos os combustíveis, sem o filtro de "não-principal" usado por <see cref="Queries.GetCombustiveis.GetCombustiveisQuery"/>
/// (que existe só para o seletor de combustível da tela de viagens). Usada pela tela administrativa.
/// </summary>
public record GetTodosCombustiveisQuery : IRequest<List<CombustivelDto>>;

public class GetTodosCombustiveisQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetTodosCombustiveisQuery, List<CombustivelDto>>
{
    public Task<List<CombustivelDto>> Handle(GetTodosCombustiveisQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Combustiveis
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .Select(c => new CombustivelDto(
                c.Codigo,
                c.Nome,
                c.UnidadeMedida,
                c.CombustivelPrincipal,
                c.CodigoCombustivelBiogenico,
                c.CombustivelBiogenico != null ? c.CombustivelBiogenico.Nome : null,
                c.CodigoCombustivelFossil,
                c.CombustivelFossil != null ? c.CombustivelFossil.Nome : null))
            .ToListAsync(cancellationToken);
    }
}
