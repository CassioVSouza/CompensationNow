using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.FatoresComposicaoCombustivel.Queries.GetFatoresComposicaoCombustivel;

public record GetFatoresComposicaoCombustivelQuery : IRequest<List<FatorComposicaoCombustivelDto>>;

public class GetFatoresComposicaoCombustivelQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetFatoresComposicaoCombustivelQuery, List<FatorComposicaoCombustivelDto>>
{
    public Task<List<FatorComposicaoCombustivelDto>> Handle(
        GetFatoresComposicaoCombustivelQuery request, CancellationToken cancellationToken)
    {
        return dbContext.FatoresComposicaoCombustivel
            .AsNoTracking()
            .OrderByDescending(f => f.Ano).ThenByDescending(f => f.Mes)
            .Select(f => new FatorComposicaoCombustivelDto(f.Codigo, f.Mes, f.Ano, f.PercentualEtanol, f.PercentualBiodiesel))
            .ToListAsync(cancellationToken);
    }
}
