using Amazon.CognitoIdentityProvider;
using CompenseAgora.Auth;
using CompenseAgora.Features.Auth.Commands.Register;
using CompenseAgora.Tests.Infrastructure;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using Moq;

namespace CompenseAgora.Tests.Features.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand ValidCommand() => new(
        Nome: "Ana",
        Sobrenome: "Silva",
        Email: "ana@example.com",
        Password: "Password1!",
        Endereco: null,
        Bairro: null,
        Numero: null,
        Cidade: null,
        Estado: null,
        Pais: null,
        Celular: null);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Nome_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { Nome = "" });
        result.ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void Empty_Sobrenome_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { Sobrenome = "" });
        result.ShouldHaveValidationErrorFor(x => x.Sobrenome);
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { Email = "not-an-email" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Empty_Email_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { Email = "" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("short1!")] // too short
    [InlineData("alllowercase1!")] // no uppercase
    [InlineData("ALLUPPERCASE1!")] // no lowercase
    [InlineData("NoDigitsHere!")] // no digit
    [InlineData("NoSpecialChar1")] // no special char
    public void Password_Not_Meeting_Policy_Fails(string password)
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = password });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_Meeting_Policy_Passes()
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = "Password1!" });
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Nome_Over_MaxLength_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { Nome = new string('a', 151) });
        result.ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void Endereco_Over_MaxLength_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { Endereco = new string('a', 251) });
        result.ShouldHaveValidationErrorFor(x => x.Endereco);
    }
}

public class RegisterCommandHandlerTests
{
    private static CognitoAuthService CreateCognitoAuthService(Mock<IAmazonCognitoIdentityProvider> mock)
    {
        var options = Options.Create(new CognitoOptions { UserPoolId = "pool", ClientId = "client" });
        return new CognitoAuthService(mock.Object, options);
    }

    [Fact]
    public async Task Handle_CreatesPessoa_WithCognitoSub_OnSuccessfulSignUp()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();

        var cognitoMock = new Mock<IAmazonCognitoIdentityProvider>();
        cognitoMock
            .Setup(c => c.SignUpAsync(It.IsAny<Amazon.CognitoIdentityProvider.Model.SignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.CognitoIdentityProvider.Model.SignUpResponse
            {
                UserSub = "cognito-sub-abc",
                UserConfirmed = false,
            });

        var handler = new RegisterCommandHandler(CreateCognitoAuthService(cognitoMock), dbContext);

        var command = new RegisterCommand(
            "Ana", "Silva", "ana@example.com", "Password1!",
            null, null, null, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Codigo > 0);
        Assert.True(result.RequiresConfirmation);

        var pessoa = await dbContext.Pessoas.FindAsync(result.Codigo);
        Assert.NotNull(pessoa);
        Assert.Equal("cognito-sub-abc", pessoa!.CognitoSub);
        Assert.Equal("ana@example.com", pessoa.Email);
    }

    [Fact]
    public async Task Handle_RequiresConfirmation_False_WhenCognitoAutoConfirms()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();

        var cognitoMock = new Mock<IAmazonCognitoIdentityProvider>();
        cognitoMock
            .Setup(c => c.SignUpAsync(It.IsAny<Amazon.CognitoIdentityProvider.Model.SignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.CognitoIdentityProvider.Model.SignUpResponse
            {
                UserSub = "cognito-sub-xyz",
                UserConfirmed = true,
            });

        var handler = new RegisterCommandHandler(CreateCognitoAuthService(cognitoMock), dbContext);

        var command = new RegisterCommand(
            "Bea", "Souza", "bea@example.com", "Password1!",
            null, null, null, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.RequiresConfirmation);
    }

    [Fact]
    public async Task Handle_DoesNotInsertPessoa_WhenCognitoSignUpFails()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();

        var cognitoMock = new Mock<IAmazonCognitoIdentityProvider>();
        cognitoMock
            .Setup(c => c.SignUpAsync(It.IsAny<Amazon.CognitoIdentityProvider.Model.SignUpRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Amazon.CognitoIdentityProvider.Model.UsernameExistsException("exists"));

        var handler = new RegisterCommandHandler(CreateCognitoAuthService(cognitoMock), dbContext);

        var command = new RegisterCommand(
            "Cid", "Gomes", "cid@example.com", "Password1!",
            null, null, null, null, null, null, null);

        await Assert.ThrowsAsync<CompenseAgora.Common.Exceptions.AuthException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Empty(dbContext.Pessoas);
    }
}
