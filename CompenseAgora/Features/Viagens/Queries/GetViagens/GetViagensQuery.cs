using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Viagens.Queries.GetViagens;

public record GetViagensQuery(int CodigoPessoa, DateOnly? DataInicio = null, DateOnly? DataFim = null) : IRequest<List<ViagemDto>>;

public class GetViagensQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetViagensQuery, List<ViagemDto>>
{
    public Task<List<ViagemDto>> Handle(GetViagensQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Viagens
            .AsNoTracking()
            .Where(v => v.CodigoPessoa == request.CodigoPessoa
                        && (request.DataInicio == null || v.DataReferencia >= request.DataInicio)
                        && (request.DataFim == null || v.DataReferencia <= request.DataFim))
            .OrderByDescending(v => v.DataReferencia)
            .Select(v => new ViagemDto(
                v.Codigo,
                v.CodigoPessoa,
                v.CodigoFrota,
                v.Frota != null ? v.Frota.Nome : null,
                v.CodigoCombustivel,
                v.Combustivel != null ? v.Combustivel.Nome : null,
                v.CriadoEm,
                v.DataReferencia,
                v.Consumo,
                v.AnoFrota,
                v.DistanciaKM,
                v.EmissaoCO2))
            .ToListAsync(cancellationToken);
    }
}
