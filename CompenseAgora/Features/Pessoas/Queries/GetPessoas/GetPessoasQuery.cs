using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Pessoas.Queries.GetPessoas;

public record GetPessoasQuery : IRequest<List<PessoaDto>>;

public class GetPessoasQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetPessoasQuery, List<PessoaDto>>
{
    public Task<List<PessoaDto>> Handle(GetPessoasQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Pessoas
            .AsNoTracking()
            .OrderBy(p => p.Nome)
            .Select(p => new PessoaDto(
                p.Codigo,
                p.Nome,
                p.Sobrenome,
                p.Email,
                p.Endereco,
                p.Bairro,
                p.Numero,
                p.Cidade,
                p.Estado,
                p.Pais,
                p.Celular))
            .ToListAsync(cancellationToken);
    }
}
