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
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("Nome deve ter no máximo 150 caracteres.");
        RuleFor(x => x.Sobrenome).NotEmpty().WithMessage("Sobrenome é obrigatório.")
            .MaximumLength(150).WithMessage("Sobrenome deve ter no máximo 150 caracteres.");
        RuleFor(x => x.Email).NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(320).WithMessage("E-mail deve ter no máximo 320 caracteres.")
            .EmailAddress().WithMessage("E-mail inválido.");
        RuleFor(x => x.Endereco).NotEmpty().WithMessage("Endereço é obrigatório.")
            .MaximumLength(250).WithMessage("Endereço deve ter no máximo 250 caracteres.");
        RuleFor(x => x.Bairro).NotEmpty().WithMessage("Bairro é obrigatório.")
            .MaximumLength(150).WithMessage("Bairro deve ter no máximo 150 caracteres.");
        RuleFor(x => x.Numero).NotEmpty().WithMessage("Número é obrigatório.")
            .MaximumLength(20).WithMessage("Número deve ter no máximo 20 caracteres.");
        RuleFor(x => x.Cidade).NotEmpty().WithMessage("Cidade é obrigatória.")
            .MaximumLength(150).WithMessage("Cidade deve ter no máximo 150 caracteres.");
        RuleFor(x => x.Estado).NotEmpty().WithMessage("Estado é obrigatório.")
            .MaximumLength(100).WithMessage("Estado deve ter no máximo 100 caracteres.");
        RuleFor(x => x.Pais).NotEmpty().WithMessage("País é obrigatório.")
            .MaximumLength(100).WithMessage("País deve ter no máximo 100 caracteres.");
        RuleFor(x => x.Celular).NotEmpty().WithMessage("Celular é obrigatório.")
            .MaximumLength(20).WithMessage("Celular deve ter no máximo 20 caracteres.");
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
