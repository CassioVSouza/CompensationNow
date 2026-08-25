using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Viagens.Queries.GetViagemById;

public record GetViagemByIdQuery(int Codigo) : IRequest<ViagemDto?>;

public class GetViagemByIdQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetViagemByIdQuery, ViagemDto?>
{
    public Task<ViagemDto?> Handle(GetViagemByIdQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Viagens
            .AsNoTracking()
            .Where(v => v.Codigo == request.Codigo)
            .Select(v => new ViagemDto(
                v.Codigo,
                v.CodigoPessoa,
                v.CodigoFrota,
                v.Frota.Nome,
                v.CodigoCombustivel,
                v.Combustivel != null ? v.Combustivel.Nome : null,
                v.CriadoEm,
                v.DataReferencia,
                v.Consumo,
                v.AnoFrota,
                v.DistanciaKM,
                v.EmissaoCO2))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
