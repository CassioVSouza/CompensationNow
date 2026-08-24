using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Pessoas.Commands.UpdatePessoa;

public record UpdatePessoaCommand(
    int Codigo,
    string Nome,
    string Sobrenome,
    string Email,
    string Endereco,
    string Bairro,
    string Numero,
    string Cidade,
    string Estado,
    string Pais,
    string Celular) : IRequest;

public class UpdatePessoaCommandValidator : AbstractValidator<UpdatePessoaCommand>
{
    public UpdatePessoaCommandValidator()
    {
        RuleFor(x => x.Codigo).GreaterThan(0);
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Sobrenome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320).EmailAddress();
        RuleFor(x => x.Endereco).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Bairro).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Numero).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Cidade).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Estado).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Pais).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Celular).NotEmpty().MaximumLength(20);
    }
}

public class UpdatePessoaCommandHandler(CompenseAgoraDbContext dbContext) : IRequestHandler<UpdatePessoaCommand>
{
    public async Task Handle(UpdatePessoaCommand request, CancellationToken cancellationToken)
    {
        var pessoa = await dbContext.Pessoas.FindAsync([request.Codigo], cancellationToken)
            ?? throw new NotFoundException(nameof(Pessoa), request.Codigo);

        pessoa.Nome = request.Nome;
        pessoa.Sobrenome = request.Sobrenome;
        pessoa.Email = request.Email;
        pessoa.Endereco = request.Endereco;
        pessoa.Bairro = request.Bairro;
        pessoa.Numero = request.Numero;
        pessoa.Cidade = request.Cidade;
        pessoa.Estado = request.Estado;
        pessoa.Pais = request.Pais;
        pessoa.Celular = request.Celular;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
