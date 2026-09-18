using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using CompenseAgora.Auth;
using CompenseAgora.Common.Exceptions;
using CompenseAgora.Features.Auth.Commands.ConfirmEmail;
using CompenseAgora.Features.Auth.Commands.ResendConfirmationCode;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using Moq;

namespace CompenseAgora.Tests.Features.Auth;

public class ConfirmEmailCommandValidatorTests
{
    private readonly ConfirmEmailCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.TestValidate(new ConfirmEmailCommand("ana@example.com", "123456"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Email_Fails()
    {
        var result = _validator.TestValidate(new ConfirmEmailCommand("", "123456"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var result = _validator.TestValidate(new ConfirmEmailCommand("not-an-email", "123456"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Empty_Code_Fails()
    {
        var result = _validator.TestValidate(new ConfirmEmailCommand("ana@example.com", ""));
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }
}

public class ConfirmEmailCommandHandlerTests
{
    private static CognitoAuthService CreateCognitoAuthService(Mock<IAmazonCognitoIdentityProvider> mock)
    {
        var options = Options.Create(new CognitoOptions { UserPoolId = "pool", ClientId = "client" });
        return new CognitoAuthService(mock.Object, options);
    }

    [Fact]
    public async Task Handle_CallsCognitoConfirmSignUp_WithGivenEmailAndCode()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        ConfirmSignUpRequest? captured = null;
        mock.Setup(c => c.ConfirmSignUpAsync(It.IsAny<ConfirmSignUpRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConfirmSignUpRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new ConfirmSignUpResponse());

        var handler = new ConfirmEmailCommandHandler(CreateCognitoAuthService(mock));

        await handler.Handle(new ConfirmEmailCommand("ana@example.com", "123456"), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("ana@example.com", captured!.Username);
        Assert.Equal("123456", captured.ConfirmationCode);
    }

    [Fact]
    public async Task Handle_PropagatesAuthException_OnCodeMismatch()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.ConfirmSignUpAsync(It.IsAny<ConfirmSignUpRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CodeMismatchException("bad code"));

        var handler = new ConfirmEmailCommandHandler(CreateCognitoAuthService(mock));

        await Assert.ThrowsAsync<AuthException>(
            () => handler.Handle(new ConfirmEmailCommand("ana@example.com", "000000"), CancellationToken.None));
    }
}

public class ResendConfirmationCodeCommandValidatorTests
{
    private readonly ResendConfirmationCodeCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.TestValidate(new ResendConfirmationCodeCommand("ana@example.com"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Email_Fails()
    {
        var result = _validator.TestValidate(new ResendConfirmationCodeCommand(""));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var result = _validator.TestValidate(new ResendConfirmationCodeCommand("not-an-email"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}

public class ResendConfirmationCodeCommandHandlerTests
{
    private static CognitoAuthService CreateCognitoAuthService(Mock<IAmazonCognitoIdentityProvider> mock)
    {
        var options = Options.Create(new CognitoOptions { UserPoolId = "pool", ClientId = "client" });
        return new CognitoAuthService(mock.Object, options);
    }

    [Fact]
    public async Task Handle_CallsCognitoResend_WithGivenEmail()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        ResendConfirmationCodeRequest? captured = null;
        mock.Setup(c => c.ResendConfirmationCodeAsync(It.IsAny<ResendConfirmationCodeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ResendConfirmationCodeRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new ResendConfirmationCodeResponse());

        var handler = new ResendConfirmationCodeCommandHandler(CreateCognitoAuthService(mock));

        await handler.Handle(new ResendConfirmationCodeCommand("ana@example.com"), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("ana@example.com", captured!.Username);
    }
}
