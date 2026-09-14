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
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Sobrenome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320).EmailAddress();
        // Mirrors Cognito's default password policy; adjust if the User Pool's policy differs.
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Senha preciso conter pelo menos uma letra em maiúsculo.")
            .Matches("[a-z]").WithMessage("Senha preciso conter pelo menos uma letra em minúsculo.")
            .Matches("[0-9]").WithMessage("Senha precisa conter pelo menos um número.")
            .Matches(@"[\^$*.\[\]{}()?\-""!@#%&/\\,><':;|_~`+=]").WithMessage("Senha precisa conter pelo menos um caractér especial.");
        RuleFor(x => x.Endereco).MaximumLength(250);
        RuleFor(x => x.Bairro).MaximumLength(150);
        RuleFor(x => x.Numero).MaximumLength(20);
        RuleFor(x => x.Cidade).MaximumLength(150);
        RuleFor(x => x.Estado).MaximumLength(100);
        RuleFor(x => x.Pais).MaximumLength(100);
        RuleFor(x => x.Celular).MaximumLength(20);
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
