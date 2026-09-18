using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.FatoresCombustivel.Queries.GetFatoresCombustivel;

public record GetFatoresCombustivelQuery : IRequest<List<FatorCombustivelDto>>;

public class GetFatoresCombustivelQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetFatoresCombustivelQuery, List<FatorCombustivelDto>>
{
    public Task<List<FatorCombustivelDto>> Handle(GetFatoresCombustivelQuery request, CancellationToken cancellationToken)
    {
        return dbContext.FatoresDoCombustivel
            .AsNoTracking()
            .OrderBy(f => f.Combustivel.Nome).ThenByDescending(f => f.Ano)
            .Select(f => new FatorCombustivelDto(
                f.Codigo,
                f.CodigoCombustivel,
                f.Combustivel.Nome,
                f.Ano,
                f.PoderCalorificoInferior,
                f.Densidade,
                f.CO2,
                f.CH4,
                f.N2O))
            .ToListAsync(cancellationToken);
    }
}
