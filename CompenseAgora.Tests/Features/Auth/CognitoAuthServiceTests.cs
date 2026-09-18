using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using CompenseAgora.Auth;
using CompenseAgora.Common.Exceptions;
using Microsoft.Extensions.Options;
using Moq;

namespace CompenseAgora.Tests.Features.Auth;

/// <summary>
/// Tests CognitoAuthService against a mocked IAmazonCognitoIdentityProvider - never real AWS calls.
/// This is the mock boundary per the testing charter: mocking here (rather than mocking CognitoAuthService
/// itself) exercises the real SECRET_HASH computation and AWS-exception-to-AuthException translation.
/// </summary>
public class CognitoAuthServiceTests
{
    private static CognitoAuthService CreateService(
        Mock<IAmazonCognitoIdentityProvider> cognitoMock,
        string? clientSecret = null)
    {
        var options = Options.Create(new CognitoOptions
        {
            UserPoolId = "pool-id",
            ClientId = "client-id",
            ClientSecret = clientSecret,
        });

        return new CognitoAuthService(cognitoMock.Object, options);
    }

    // ---------- SignUpAsync ----------

    [Fact]
    public async Task SignUpAsync_ReturnsSubAndConfirmedFlag_OnSuccess()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.SignUpAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse { UserSub = "sub-123", UserConfirmed = true });

        var service = CreateService(mock);

        var result = await service.SignUpAsync("user@example.com", "Password1!", CancellationToken.None);

        Assert.Equal("sub-123", result.Sub);
        Assert.True(result.UserConfirmed);
    }

    [Fact]
    public async Task SignUpAsync_WithoutClientSecret_DoesNotSetSecretHash()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        SignUpRequest? capturedRequest = null;
        mock.Setup(c => c.SignUpAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SignUpRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new SignUpResponse { UserSub = "sub-123", UserConfirmed = false });

        var service = CreateService(mock, clientSecret: null);

        await service.SignUpAsync("user@example.com", "Password1!", CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Null(capturedRequest!.SecretHash);
    }

    [Fact]
    public async Task SignUpAsync_WithClientSecret_ComputesSecretHash()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        SignUpRequest? capturedRequest = null;
        mock.Setup(c => c.SignUpAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SignUpRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new SignUpResponse { UserSub = "sub-123", UserConfirmed = false });

        var service = CreateService(mock, clientSecret: "shh-secret");

        await service.SignUpAsync("user@example.com", "Password1!", CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.False(string.IsNullOrEmpty(capturedRequest!.SecretHash));
    }

    [Fact]
    public async Task SignUpAsync_UsernameExists_ThrowsAuthException()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.SignUpAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsernameExistsException("already exists"));

        var service = CreateService(mock);

        var ex = await Assert.ThrowsAsync<AuthException>(
            () => service.SignUpAsync("user@example.com", "Password1!", CancellationToken.None));
        Assert.Contains("Já existe uma conta cadastrada com este e-mail", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SignUpAsync_InvalidPassword_ThrowsAuthException()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.SignUpAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidPasswordException("too weak"));

        var service = CreateService(mock);

        var ex = await Assert.ThrowsAsync<AuthException>(
            () => service.SignUpAsync("user@example.com", "weak", CancellationToken.None));
        Assert.Contains("A senha não atende aos requisitos", ex.Message);
    }

    [Fact]
    public async Task SignUpAsync_GenericCognitoException_ThrowsAuthException()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.SignUpAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TooManyRequestsException("slow down"));

        var service = CreateService(mock);

        await Assert.ThrowsAsync<AuthException>(
            () => service.SignUpAsync("user@example.com", "Password1!", CancellationToken.None));
    }

    // ---------- ConfirmSignUpAsync ----------

    [Fact]
    public async Task ConfirmSignUpAsync_Succeeds_WhenCognitoAccepts()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.ConfirmSignUpAsync(It.IsAny<ConfirmSignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfirmSignUpResponse());

        var service = CreateService(mock);

        await service.ConfirmSignUpAsync("user@example.com", "123456", CancellationToken.None);

        mock.Verify(c => c.ConfirmSignUpAsync(It.IsAny<ConfirmSignUpRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmSignUpAsync_CodeMismatch_ThrowsAuthException()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.ConfirmSignUpAsync(It.IsAny<ConfirmSignUpRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CodeMismatchException("bad code"));

        var service = CreateService(mock);

        var ex = await Assert.ThrowsAsync<AuthException>(
            () => service.ConfirmSignUpAsync("user@example.com", "000000", CancellationToken.None));
        Assert.Contains("Código de confirmação inválido", ex.Message);
    }

    [Fact]
    public async Task ConfirmSignUpAsync_ExpiredCode_ThrowsAuthException()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.ConfirmSignUpAsync(It.IsAny<ConfirmSignUpRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExpiredCodeException("expired"));

        var service = CreateService(mock);

        var ex = await Assert.ThrowsAsync<AuthException>(
            () => service.ConfirmSignUpAsync("user@example.com", "000000", CancellationToken.None));
        Assert.Contains("expirado", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- ResendConfirmationCodeAsync ----------

    [Fact]
    public async Task ResendConfirmationCodeAsync_Succeeds_WhenCognitoAccepts()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.ResendConfirmationCodeAsync(It.IsAny<ResendConfirmationCodeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendConfirmationCodeResponse());

        var service = CreateService(mock);

        await service.ResendConfirmationCodeAsync("user@example.com", CancellationToken.None);

        mock.Verify(
            c => c.ResendConfirmationCodeAsync(It.IsAny<ResendConfirmationCodeRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResendConfirmationCodeAsync_GenericException_ThrowsAuthException()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.ResendConfirmationCodeAsync(It.IsAny<ResendConfirmationCodeRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LimitExceededException("too many"));

        var service = CreateService(mock);

        await Assert.ThrowsAsync<AuthException>(
            () => service.ResendConfirmationCodeAsync("user@example.com", CancellationToken.None));
    }

    // ---------- LoginAsync ----------

    [Fact]
    public async Task LoginAsync_ReturnsTokens_OnSuccess()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InitiateAuthResponse
            {
                AuthenticationResult = new AuthenticationResultType
                {
                    AccessToken = "access-token",
                    IdToken = "id-token",
                    RefreshToken = "refresh-token",
                },
            });

        var service = CreateService(mock);

        var result = await service.LoginAsync("user@example.com", "Password1!", CancellationToken.None);

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("id-token", result.IdToken);
        Assert.Equal("refresh-token", result.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_WithoutClientSecret_DoesNotSetSecretHashParameter()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        InitiateAuthRequest? capturedRequest = null;
        mock.Setup(c => c.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InitiateAuthRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new InitiateAuthResponse
            {
                AuthenticationResult = new AuthenticationResultType
                {
                    AccessToken = "a",
                    IdToken = "i",
                    RefreshToken = "r",
                },
            });

        var service = CreateService(mock, clientSecret: null);

        await service.LoginAsync("user@example.com", "Password1!", CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.False(capturedRequest!.AuthParameters.ContainsKey("SECRET_HASH"));
    }

    [Fact]
    public async Task LoginAsync_WithClientSecret_SetsSecretHashParameter()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        InitiateAuthRequest? capturedRequest = null;
        mock.Setup(c => c.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InitiateAuthRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new InitiateAuthResponse
            {
                AuthenticationResult = new AuthenticationResultType
                {
                    AccessToken = "a",
                    IdToken = "i",
                    RefreshToken = "r",
                },
            });

        var service = CreateService(mock, clientSecret: "shh-secret");

        await service.LoginAsync("user@example.com", "Password1!", CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.AuthParameters.ContainsKey("SECRET_HASH"));
        Assert.False(string.IsNullOrEmpty(capturedRequest.AuthParameters["SECRET_HASH"]));
    }

    [Fact]
    public async Task LoginAsync_NullAuthenticationResult_ThrowsAuthException()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InitiateAuthResponse
            {
                AuthenticationResult = null,
                ChallengeName = ChallengeNameType.SMS_MFA,
            });

        var service = CreateService(mock);

        var ex = await Assert.ThrowsAsync<AuthException>(
            () => service.LoginAsync("user@example.com", "Password1!", CancellationToken.None));
        Assert.Contains("Etapa adicional de login necessária", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_UserNotConfirmed_ThrowsAuthException()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UserNotConfirmedException("not confirmed"));

        var service = CreateService(mock);

        var ex = await Assert.ThrowsAsync<AuthException>(
            () => service.LoginAsync("user@example.com", "Password1!", CancellationToken.None));
        Assert.Contains("Confirme seu e-mail", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_NotAuthorized_ThrowsAuthException()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotAuthorizedException("bad creds"));

        var service = CreateService(mock);

        var ex = await Assert.ThrowsAsync<AuthException>(
            () => service.LoginAsync("user@example.com", "WrongPassword1!", CancellationToken.None));
        Assert.Contains("E-mail ou senha inválidos", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsAuthException()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UserNotFoundException("no such user"));

        var service = CreateService(mock);

        var ex = await Assert.ThrowsAsync<AuthException>(
            () => service.LoginAsync("nobody@example.com", "Password1!", CancellationToken.None));
        Assert.Contains("E-mail ou senha inválidos", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_GenericCognitoException_ThrowsAuthException()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TooManyRequestsException("slow down"));

        var service = CreateService(mock);

        await Assert.ThrowsAsync<AuthException>(
            () => service.LoginAsync("user@example.com", "Password1!", CancellationToken.None));
    }
}
