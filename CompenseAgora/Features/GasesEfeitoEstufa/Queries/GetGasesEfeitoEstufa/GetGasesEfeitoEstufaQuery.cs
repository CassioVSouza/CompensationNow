using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.GasesEfeitoEstufa.Queries.GetGasesEfeitoEstufa;

public record GetGasesEfeitoEstufaQuery : IRequest<List<GasEfeitoEstufaDto>>;

public class GetGasesEfeitoEstufaQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetGasesEfeitoEstufaQuery, List<GasEfeitoEstufaDto>>
{
    public Task<List<GasEfeitoEstufaDto>> Handle(GetGasesEfeitoEstufaQuery request, CancellationToken cancellationToken)
    {
        return dbContext.GasesEfeitoEstufa
            .AsNoTracking()
            .OrderBy(g => g.Nome)
            .Select(g => new GasEfeitoEstufaDto(g.Codigo, g.Nome, g.Familia, g.GWP))
            .ToListAsync(cancellationToken);
    }
}
