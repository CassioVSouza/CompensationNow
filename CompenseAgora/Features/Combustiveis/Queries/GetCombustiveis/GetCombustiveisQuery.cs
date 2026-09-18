using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Combustiveis.Queries.GetCombustiveis;

public record GetCombustiveisQuery : IRequest<List<CombustivelDto>>;

public class GetCombustiveisQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetCombustiveisQuery, List<CombustivelDto>>
{
    public Task<List<CombustivelDto>> Handle(GetCombustiveisQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Combustiveis
            .AsNoTracking()
            .Where(c => !c.CombustivelPrincipal)
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
