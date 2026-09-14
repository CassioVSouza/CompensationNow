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
        RuleFor(x => x.Codigo).GreaterThan(0).WithMessage("Código deve ser maior que zero.");
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
