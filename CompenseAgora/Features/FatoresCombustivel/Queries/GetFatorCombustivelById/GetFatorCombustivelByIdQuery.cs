using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.FatoresCombustivel.Queries.GetFatorCombustivelById;

public record GetFatorCombustivelByIdQuery(int Codigo) : IRequest<FatorCombustivelDto?>;

public class GetFatorCombustivelByIdQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetFatorCombustivelByIdQuery, FatorCombustivelDto?>
{
    public Task<FatorCombustivelDto?> Handle(GetFatorCombustivelByIdQuery request, CancellationToken cancellationToken)
    {
        return dbContext.FatoresDoCombustivel
            .AsNoTracking()
            .Where(f => f.Codigo == request.Codigo)
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
