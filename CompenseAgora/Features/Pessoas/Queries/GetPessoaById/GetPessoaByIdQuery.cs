using CompenseAgora.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Pessoas.Queries.GetPessoaById;

public record GetPessoaByIdQuery(int Codigo) : IRequest<PessoaDto?>;

public class GetPessoaByIdQueryHandler(CompenseAgoraDbContext dbContext)
    : IRequestHandler<GetPessoaByIdQuery, PessoaDto?>
{
    public Task<PessoaDto?> Handle(GetPessoaByIdQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Pessoas
            .AsNoTracking()
            .Where(p => p.Codigo == request.Codigo)
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
