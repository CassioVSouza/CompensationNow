using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.FatoresEnergia.Queries.GetFatorEnergiaById;

public record GetFatorEnergiaByIdQuery(int Codigo) : IRequest<FatorEnergiaDto?>;

public class GetFatorEnergiaByIdQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetFatorEnergiaByIdQuery, FatorEnergiaDto?>
{
    public Task<FatorEnergiaDto?> Handle(GetFatorEnergiaByIdQuery request, CancellationToken cancellationToken)
    {
        return dbContext.FatoresEnergia
            .AsNoTracking()
            .Where(f => f.Codigo == request.Codigo)
            .Select(f => new FatorEnergiaDto(f.Codigo, f.Mes, f.Ano, f.FeSin))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
