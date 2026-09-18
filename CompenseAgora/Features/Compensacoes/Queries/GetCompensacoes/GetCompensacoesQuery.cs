using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Compensacoes.Queries.GetCompensacoes;

public record GetCompensacoesQuery(int CodigoPessoa) : IRequest<List<CompensacaoDto>>;

public class GetCompensacoesQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetCompensacoesQuery, List<CompensacaoDto>>
{
    public Task<List<CompensacaoDto>> Handle(GetCompensacoesQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Compensacoes
            .AsNoTracking()
            .Where(c => c.CodigoPessoa == request.CodigoPessoa)
            .OrderByDescending(c => c.DataReferencia)
            .Select(c => new CompensacaoDto(
                c.Codigo,
                c.CodigoPessoa,
                c.CriadoEm,
                c.DataReferencia,
                c.TipoCompensacao,
                c.QuantidadeCompensada))
            .ToListAsync(cancellationToken);
    }
}
