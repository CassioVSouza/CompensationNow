using CompenseAgora.Auth;
using CompenseAgora.Data;
using CompenseAgora.Entities;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Nome,
    string Sobrenome,
    string Email,
    string Password,
    string? Endereco,
    string? Bairro,
    string? Numero,
    string? Cidade,
    string? Estado,
    string? Pais,
    string? Celular) : IRequest<RegisterResult>;

public record RegisterResult(int Codigo, bool RequiresConfirmation);

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("Nome deve ter no máximo 150 caracteres.");
        RuleFor(x => x.Sobrenome).NotEmpty().WithMessage("Sobrenome é obrigatório.")
            .MaximumLength(150).WithMessage("Sobrenome deve ter no máximo 150 caracteres.");
        RuleFor(x => x.Email).NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(320).WithMessage("E-mail deve ter no máximo 320 caracteres.")
            .EmailAddress().WithMessage("E-mail inválido.");
        // Mirrors Cognito's default password policy; adjust if the User Pool's policy differs.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
            .Matches("[A-Z]").WithMessage("A senha precisa conter pelo menos uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("A senha precisa conter pelo menos uma letra minúscula.")
            .Matches("[0-9]").WithMessage("A senha precisa conter pelo menos um número.")
            .Matches(@"[\^$*.\[\]{}()?\-""!@#%&/\\,><':;|_~`+=]").WithMessage("A senha precisa conter pelo menos um caractere especial.");
        RuleFor(x => x.Endereco).MaximumLength(250).WithMessage("Endereço deve ter no máximo 250 caracteres.");
        RuleFor(x => x.Bairro).MaximumLength(150).WithMessage("Bairro deve ter no máximo 150 caracteres.");
        RuleFor(x => x.Numero).MaximumLength(20).WithMessage("Número deve ter no máximo 20 caracteres.");
        RuleFor(x => x.Cidade).MaximumLength(150).WithMessage("Cidade deve ter no máximo 150 caracteres.");
        RuleFor(x => x.Estado).MaximumLength(100).WithMessage("Estado deve ter no máximo 100 caracteres.");
        RuleFor(x => x.Pais).MaximumLength(100).WithMessage("País deve ter no máximo 100 caracteres.");
        RuleFor(x => x.Celular).MaximumLength(20).WithMessage("Celular deve ter no máximo 20 caracteres.");
    }
}

public class RegisterCommandHandler(CognitoAuthService cognitoAuth, CompenseAgoraDbContext dbContext)
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Cognito is the source of truth for the credential; Pessoa only exists once Cognito accepts the sign-up.
        // If the DB insert below fails after this succeeds, the Cognito user is left orphaned (no automatic
        // rollback) — acceptable for now, but worth a cleanup job if it becomes a real problem.
        var signUpResult = await cognitoAuth.SignUpAsync(request.Email, request.Password, cancellationToken);

        var pessoa = new Pessoa
        {
            Nome = request.Nome,
            Sobrenome = request.Sobrenome,
            Email = request.Email,
            CognitoSub = signUpResult.Sub,
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

        return new RegisterResult(pessoa.Codigo, RequiresConfirmation: !signUpResult.UserConfirmed);
    }
}
