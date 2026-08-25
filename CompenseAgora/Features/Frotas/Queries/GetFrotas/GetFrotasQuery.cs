using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Frotas.Queries.GetFrotas;

public record GetFrotasQuery : IRequest<List<FrotaDto>>;

public class GetFrotasQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetFrotasQuery, List<FrotaDto>>
{
    public Task<List<FrotaDto>> Handle(GetFrotasQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Frotas
            .AsNoTracking()
            .OrderBy(f => f.Nome)
            .Select(f => new FrotaDto(f.Codigo, f.Nome, f.CodigoCombustivelPrimario))
            .ToListAsync(cancellationToken);
    }
}
