using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Frotas.Queries.GetFrotaById;

public record GetFrotaByIdQuery(int Codigo) : IRequest<FrotaDto?>;

public class GetFrotaByIdQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetFrotaByIdQuery, FrotaDto?>
{
    public Task<FrotaDto?> Handle(GetFrotaByIdQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Frotas
            .AsNoTracking()
            .Where(f => f.Codigo == request.Codigo)
            .Select(f => new FrotaDto(
                f.Codigo,
                f.Nome,
                f.CodigoCombustivelPrimario,
                f.CombustivelPrimario.Nome,
                f.CodigoCombustivelBiogenico,
                f.CombustivelBiogenico != null ? f.CombustivelBiogenico.Nome : null,
                f.CodigoCombustivelFossil,
                f.CombustivelFossil != null ? f.CombustivelFossil.Nome : null))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
