using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Combustiveis.Queries.GetCombustivelById;

public record GetCombustivelByIdQuery(int Codigo) : IRequest<CombustivelDto?>;

public class GetCombustivelByIdQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetCombustivelByIdQuery, CombustivelDto?>
{
    public Task<CombustivelDto?> Handle(GetCombustivelByIdQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Combustiveis
            .AsNoTracking()
            .Where(c => c.Codigo == request.Codigo)
            .Select(c => new CombustivelDto(
                c.Codigo,
                c.Nome,
                c.UnidadeMedida,
                c.CombustivelPrincipal,
                c.CodigoCombustivelBiogenico,
                c.CombustivelBiogenico != null ? c.CombustivelBiogenico.Nome : null,
                c.CodigoCombustivelFossil,
                c.CombustivelFossil != null ? c.CombustivelFossil.Nome : null))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
