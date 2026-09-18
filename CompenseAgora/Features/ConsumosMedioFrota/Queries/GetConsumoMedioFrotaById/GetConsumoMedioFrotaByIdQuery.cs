using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.ConsumosMedioFrota.Queries.GetConsumoMedioFrotaById;

public record GetConsumoMedioFrotaByIdQuery(int Codigo) : IRequest<ConsumoMedioFrotaDto?>;

public class GetConsumoMedioFrotaByIdQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetConsumoMedioFrotaByIdQuery, ConsumoMedioFrotaDto?>
{
    public Task<ConsumoMedioFrotaDto?> Handle(GetConsumoMedioFrotaByIdQuery request, CancellationToken cancellationToken)
    {
        return dbContext.ConsumosMedioFrota
            .AsNoTracking()
            .Where(c => c.Codigo == request.Codigo)
            .Select(c => new ConsumoMedioFrotaDto(
                c.Codigo,
                c.CodigoFrota,
                c.Frota.Nome,
                c.ParaTodos,
                c.Ano,
                c.Consumo,
                c.UnidadeMedida))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
