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
            .OrderBy(c => c.Nome)
            .Select(c => new CombustivelDto(c.Codigo, c.Nome, c.UnidadeMedida))
            .ToListAsync(cancellationToken);
    }
}
