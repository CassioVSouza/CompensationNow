using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Energias.Queries.GetEnergias;

public record GetEnergiasQuery(int CodigoPessoa) : IRequest<List<EnergiaDto>>;

public class GetEnergiasQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetEnergiasQuery, List<EnergiaDto>>
{
    public Task<List<EnergiaDto>> Handle(GetEnergiasQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Energias
            .AsNoTracking()
            .Where(e => e.CodigoPessoa == request.CodigoPessoa)
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
