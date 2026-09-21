using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using CompenseAgora.Auth;
using CompenseAgora.Common.Exceptions;
using CompenseAgora.Entities;
using CompenseAgora.Features.Auth.Commands.Login;
using CompenseAgora.Tests.Infrastructure;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using Moq;

namespace CompenseAgora.Tests.Features.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.TestValidate(new LoginCommand("ana@example.com", "Password1!"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Email_Fails()
    {
        var result = _validator.TestValidate(new LoginCommand("", "Password1!"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var result = _validator.TestValidate(new LoginCommand("not-an-email", "Password1!"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Empty_Password_Fails()
    {
        var result = _validator.TestValidate(new LoginCommand("ana@example.com", ""));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}

public class LoginCommandHandlerTests
{
    private static CognitoAuthService CreateCognitoAuthService(Mock<IAmazonCognitoIdentityProvider> mock)
    {
        var options = Options.Create(new CognitoOptions { UserPoolId = "pool", ClientId = "client" });
        return new CognitoAuthService(mock.Object, options);
    }

    private static Mock<IAmazonCognitoIdentityProvider> MockSuccessfulLogin()
    {
        var mock = new Mock<IAmazonCognitoIdentityProvider>();
        mock.Setup(c => c.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InitiateAuthResponse
            {
                AuthenticationResult = new AuthenticationResultType
                {
                    AccessToken = "access",
                    IdToken = "id",
                    RefreshToken = "refresh",
                },
            });
        return mock;
    }

    [Fact]
    public async Task Handle_LooksUpPessoaByEmail_NotByDecodingToken()
    {
        // Regression test: LoginCommandHandler must resolve the local Pessoa by the Email the caller
        // supplied (validated against Cognito), not by decoding claims out of the returned JWT.
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();

        var pessoa = new Pessoa
        {
            Nome = "Ana",
            Sobrenome = "Silva",
            Email = "ana@example.com",
            CognitoSub = "sub-ana",
        };
        seedContext.Pessoas.Add(pessoa);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var cognitoMock = MockSuccessfulLogin();
        var handler = new LoginCommandHandler(CreateCognitoAuthService(cognitoMock), dbContext);

        var result = await handler.Handle(new LoginCommand("ana@example.com", "Password1!"), CancellationToken.None);

        Assert.Equal(pessoa.Codigo, result.Codigo);
        Assert.Equal("Ana", result.Nome);
        Assert.Equal("Silva", result.Sobrenome);
        Assert.Equal("ana@example.com", result.Email);
        Assert.Equal("sub-ana", result.CognitoSub);
        Assert.False(result.Admin);
    }

    [Fact]
    public async Task Handle_PropagatesAdminFlag_WhenPessoaIsAdmin()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();

        var pessoa = new Pessoa
        {
            Nome = "Ana",
            Sobrenome = "Silva",
            Email = "ana@example.com",
            CognitoSub = "sub-ana",
            Admin = true,
        };
        seedContext.Pessoas.Add(pessoa);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var cognitoMock = MockSuccessfulLogin();
        var handler = new LoginCommandHandler(CreateCognitoAuthService(cognitoMock), dbContext);

        var result = await handler.Handle(new LoginCommand("ana@example.com", "Password1!"), CancellationToken.None);

        Assert.True(result.Admin);
    }

    [Fact]
    public async Task Handle_ThrowsAuthException_WhenCognitoSucceedsButNoMatchingPessoa()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();

        var cognitoMock = MockSuccessfulLogin();
        var handler = new LoginCommandHandler(CreateCognitoAuthService(cognitoMock), dbContext);

        await Assert.ThrowsAsync<AuthException>(
            () => handler.Handle(new LoginCommand("ghost@example.com", "Password1!"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PropagatesAuthException_WhenCognitoRejectsCredentials()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();

        var cognitoMock = new Mock<IAmazonCognitoIdentityProvider>();
        cognitoMock
            .Setup(c => c.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotAuthorizedException("bad creds"));

        var handler = new LoginCommandHandler(CreateCognitoAuthService(cognitoMock), dbContext);

        var ex = await Assert.ThrowsAsync<AuthException>(
            () => handler.Handle(new LoginCommand("ana@example.com", "WrongPassword1!"), CancellationToken.None));
        Assert.Contains("E-mail ou senha inválidos", ex.Message);
    }
}
