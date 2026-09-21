using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Energias.Queries.GetEnergias;

public record GetEnergiasQuery(int CodigoPessoa, DateOnly? DataInicio = null, DateOnly? DataFim = null) : IRequest<List<EnergiaDto>>;

public class GetEnergiasQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetEnergiasQuery, List<EnergiaDto>>
{
    public Task<List<EnergiaDto>> Handle(GetEnergiasQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Energias
            .AsNoTracking()
            .Where(e => e.CodigoPessoa == request.CodigoPessoa
                        && (request.DataInicio == null || e.DataReferencia >= request.DataInicio)
                        && (request.DataFim == null || e.DataReferencia <= request.DataFim))
            .OrderByDescending(e => e.DataReferencia)
            .Select(e => new EnergiaDto(
                e.Codigo,
                e.CodigoPessoa,
                e.CriadoEm,
                e.DataReferencia,
                e.Quantidade,
                e.EmissaoCO2))
            .ToListAsync(cancellationToken);
    }
}
