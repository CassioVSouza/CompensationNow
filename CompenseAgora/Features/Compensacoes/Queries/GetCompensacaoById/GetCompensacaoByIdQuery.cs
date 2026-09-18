using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Compensacoes.Queries.GetCompensacaoById;

public record GetCompensacaoByIdQuery(int Codigo) : IRequest<CompensacaoDto?>;

public class GetCompensacaoByIdQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetCompensacaoByIdQuery, CompensacaoDto?>
{
    public Task<CompensacaoDto?> Handle(GetCompensacaoByIdQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Compensacoes
            .AsNoTracking()
            .Where(c => c.Codigo == request.Codigo)
            .Select(c => new CompensacaoDto(
                c.Codigo,
                c.CodigoPessoa,
                c.CriadoEm,
                c.DataReferencia,
                c.TipoCompensacao,
                c.QuantidadeCompensada))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
