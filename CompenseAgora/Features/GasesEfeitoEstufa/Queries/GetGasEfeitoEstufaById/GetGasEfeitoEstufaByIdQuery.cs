using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.GasesEfeitoEstufa.Queries.GetGasEfeitoEstufaById;

public record GetGasEfeitoEstufaByIdQuery(int Codigo) : IRequest<GasEfeitoEstufaDto?>;

public class GetGasEfeitoEstufaByIdQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetGasEfeitoEstufaByIdQuery, GasEfeitoEstufaDto?>
{
    public Task<GasEfeitoEstufaDto?> Handle(GetGasEfeitoEstufaByIdQuery request, CancellationToken cancellationToken)
    {
        return dbContext.GasesEfeitoEstufa
            .AsNoTracking()
            .Where(g => g.Codigo == request.Codigo)
            .Select(g => new GasEfeitoEstufaDto(g.Codigo, g.Nome, g.Familia, g.GWP))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
