using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.FatoresComposicaoCombustivel.Queries.GetFatorComposicaoCombustivelById;

public record GetFatorComposicaoCombustivelByIdQuery(int Codigo) : IRequest<FatorComposicaoCombustivelDto?>;

public class GetFatorComposicaoCombustivelByIdQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetFatorComposicaoCombustivelByIdQuery, FatorComposicaoCombustivelDto?>
{
    public Task<FatorComposicaoCombustivelDto?> Handle(
        GetFatorComposicaoCombustivelByIdQuery request, CancellationToken cancellationToken)
    {
        return dbContext.FatoresComposicaoCombustivel
            .AsNoTracking()
            .Where(f => f.Codigo == request.Codigo)
            .Select(f => new FatorComposicaoCombustivelDto(f.Codigo, f.Mes, f.Ano, f.PercentualEtanol, f.PercentualBiodiesel))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
