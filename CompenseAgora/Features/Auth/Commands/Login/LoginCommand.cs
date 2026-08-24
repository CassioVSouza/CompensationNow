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
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
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
            ?? throw new AuthException("Your credentials are valid, but no profile was found for this account. Please contact support.");

        return new LoginResult(pessoa.Codigo, pessoa.Nome, pessoa.Sobrenome, pessoa.Email, pessoa.CognitoSub);
    }
}
