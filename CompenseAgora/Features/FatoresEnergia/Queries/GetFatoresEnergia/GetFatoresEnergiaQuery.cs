using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.FatoresEnergia.Queries.GetFatoresEnergia;

public record GetFatoresEnergiaQuery : IRequest<List<FatorEnergiaDto>>;

public class GetFatoresEnergiaQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetFatoresEnergiaQuery, List<FatorEnergiaDto>>
{
    public Task<List<FatorEnergiaDto>> Handle(GetFatoresEnergiaQuery request, CancellationToken cancellationToken)
    {
        return dbContext.FatoresEnergia
            .AsNoTracking()
            .OrderByDescending(f => f.Ano).ThenByDescending(f => f.Mes)
            .Select(f => new FatorEnergiaDto(f.Codigo, f.Mes, f.Ano, f.FeSin))
            .ToListAsync(cancellationToken);
    }
}
