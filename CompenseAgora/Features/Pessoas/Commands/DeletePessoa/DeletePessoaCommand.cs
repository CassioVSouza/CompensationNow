using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Pessoas.Commands.DeletePessoa;

public record DeletePessoaCommand(int Codigo) : IRequest;

public class DeletePessoaCommandValidator : AbstractValidator<DeletePessoaCommand>
{
    public DeletePessoaCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0);
    }
}

public class DeletePessoaCommandHandler(CompenseAgoraDbContext dbContext) : IRequestHandler<DeletePessoaCommand>
{
    public async Task Handle(DeletePessoaCommand request, CancellationToken cancellationToken)
    {
        var pessoa = await dbContext.Pessoas.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(Pessoa), request.Codigo);

        dbContext.Pessoas.Remove(pessoa);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
