using CompenseAgora.Auth;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Auth.Commands.ResendConfirmationCode;

public record ResendConfirmationCodeCommand(string Email) : IRequest;

public class ResendConfirmationCodeCommandValidator : AbstractValidator<ResendConfirmationCodeCommand>
{
    public ResendConfirmationCodeCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.");
    }
}

public class ResendConfirmationCodeCommandHandler(CognitoAuthService cognitoAuth)
    : IRequestHandler<ResendConfirmationCodeCommand>
{
    public Task Handle(ResendConfirmationCodeCommand request, CancellationToken cancellationToken)
        => cognitoAuth.ResendConfirmationCodeAsync(request.Email, cancellationToken);
}
