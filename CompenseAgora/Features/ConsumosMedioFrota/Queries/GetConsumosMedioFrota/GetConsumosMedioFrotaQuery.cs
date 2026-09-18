using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.ConsumosMedioFrota.Queries.GetConsumosMedioFrota;

public record GetConsumosMedioFrotaQuery : IRequest<List<ConsumoMedioFrotaDto>>;

public class GetConsumosMedioFrotaQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetConsumosMedioFrotaQuery, List<ConsumoMedioFrotaDto>>
{
    public Task<List<ConsumoMedioFrotaDto>> Handle(GetConsumosMedioFrotaQuery request, CancellationToken cancellationToken)
    {
        return dbContext.ConsumosMedioFrota
            .AsNoTracking()
            .OrderBy(c => c.Frota.Nome).ThenByDescending(c => c.Ano)
            .Select(c => new ConsumoMedioFrotaDto(
                c.Codigo,
                c.CodigoFrota,
                c.Frota.Nome,
                c.ParaTodos,
                c.Ano,
                c.Consumo,
                c.UnidadeMedida))
            .ToListAsync(cancellationToken);
    }
}
