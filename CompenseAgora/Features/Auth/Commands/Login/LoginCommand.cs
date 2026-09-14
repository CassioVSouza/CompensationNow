using CompenseAgora.Auth;
using CompenseAgora.Common.Exceptions;
using CompenseAgora.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(int Codigo, string Nome, string Sobrenome, string Email, string CognitoSub);

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Senha é obrigatória.");
    }
}

public class LoginCommandHandler(CognitoAuthService cognitoAuth, CompenseAgoraDbContext dbContext)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Confirms the credentials against Cognito; throws AuthException on failure.
        await cognitoAuth.LoginAsync(request.Email, request.Password, cancellationToken);

        var pessoa = await dbContext.Pessoas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Email == request.Email, cancellationToken)
            ?? throw new AuthException("Suas credenciais são válidas, mas nenhum perfil foi encontrado para esta conta. Entre em contato com o suporte.");

        return new LoginResult(pessoa.Codigo, pessoa.Nome, pessoa.Sobrenome, pessoa.Email, pessoa.CognitoSub);
    }
}
