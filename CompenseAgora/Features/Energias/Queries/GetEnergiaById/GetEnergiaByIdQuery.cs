using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Energias.Queries.GetEnergiaById;

public record GetEnergiaByIdQuery(int Codigo) : IRequest<EnergiaDto?>;

public class GetEnergiaByIdQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetEnergiaByIdQuery, EnergiaDto?>
{
    public Task<EnergiaDto?> Handle(GetEnergiaByIdQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Energias
            .AsNoTracking()
            .Where(e => e.Codigo == request.Codigo)
            .Select(e => new EnergiaDto(
                e.Codigo,
                e.CodigoPessoa,
                e.CriadoEm,
                e.DataReferencia,
                e.Quantidade,
                e.EmissaoCO2))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
