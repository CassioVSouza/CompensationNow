using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Pessoas.Commands.CreatePessoa;

public record CreatePessoaCommand(
    string Nome,
    string Sobrenome,
    string Email,
    string Endereco,
    string Bairro,
    string Numero,
    string Cidade,
    string Estado,
    string Pais,
    string Celular) : IRequest<int>;

public class CreatePessoaCommandValidator : AbstractValidator<CreatePessoaCommand>
{
    public CreatePessoaCommandValidator()
    {
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

public class CreatePessoaCommandHandler(CompenseAgoraDbContext dbContext) : IRequestHandler<CreatePessoaCommand, int>
{
    public async Task<int> Handle(CreatePessoaCommand request, CancellationToken cancellationToken)
    {
        var pessoa = new Pessoa
        {
            Nome = request.Nome,
            Sobrenome = request.Sobrenome,
            Email = request.Email,
            Endereco = request.Endereco,
            Bairro = request.Bairro,
            Numero = request.Numero,
            Cidade = request.Cidade,
            Estado = request.Estado,
            Pais = request.Pais,
            Celular = request.Celular,
        };

        dbContext.Pessoas.Add(pessoa);
        await dbContext.SaveChangesAsync(cancellationToken);

        return pessoa.Codigo;
    }
}
