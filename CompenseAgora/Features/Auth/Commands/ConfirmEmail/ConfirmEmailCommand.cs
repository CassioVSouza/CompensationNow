using CompenseAgora.Auth;
using FluentValidation;
using MediatR;

namespace CompenseAgora.Features.Auth.Commands.ConfirmEmail;

public record ConfirmEmailCommand(string Email, string Code) : IRequest;

public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.");
        RuleFor(x => x.Code).NotEmpty().WithMessage("Código de confirmação é obrigatório.");
    }
}

public class ConfirmEmailCommandHandler(CognitoAuthService cognitoAuth) : IRequestHandler<ConfirmEmailCommand>
{
    public Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        => cognitoAuth.ConfirmSignUpAsync(request.Email, request.Code, cancellationToken);
}
