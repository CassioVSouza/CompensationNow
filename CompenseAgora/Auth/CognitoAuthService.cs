using System.Security.Cryptography;
using System.Text;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using CompenseAgora.Common.Exceptions;
using Microsoft.Extensions.Options;

namespace CompenseAgora.Auth;

public record CognitoSignUpResult(string Sub, bool UserConfirmed);

public record CognitoAuthResult(string AccessToken, string IdToken, string RefreshToken);

/// <summary>Thin wrapper around the Cognito Identity Provider API so the rest of the app never touches the AWS SDK directly.</summary>
public class CognitoAuthService(IAmazonCognitoIdentityProvider cognito, IOptions<CognitoOptions> options)
{
    private readonly CognitoOptions _options = options.Value;

    public async Task<CognitoSignUpResult> SignUpAsync(string email, string password, CancellationToken cancellationToken)
    {
        var request = new SignUpRequest
        {
            ClientId = _options.ClientId,
            Username = email,
            Password = password,
            SecretHash = ComputeSecretHash(email),
            UserAttributes = [new AttributeType { Name = "email", Value = email }],
        };

        try
        {
            var response = await cognito.SignUpAsync(request, cancellationToken);
            return new CognitoSignUpResult(response.UserSub, response.UserConfirmed ?? false);
        }
        catch (UsernameExistsException)
        {
            throw new AuthException("Já existe uma conta cadastrada com este e-mail.");
        }
        catch (InvalidPasswordException ex)
        {
            throw new AuthException($"A senha não atende aos requisitos: {ex.Message}");
        }
        catch (AmazonCognitoIdentityProviderException ex)
        {
            throw new AuthException(ex.Message);
        }
    }

    public async Task ConfirmSignUpAsync(string email, string confirmationCode, CancellationToken cancellationToken)
    {
        var request = new ConfirmSignUpRequest
        {
            ClientId = _options.ClientId,
            Username = email,
            ConfirmationCode = confirmationCode,
            SecretHash = ComputeSecretHash(email),
        };

        try
        {
            await cognito.ConfirmSignUpAsync(request, cancellationToken);
        }
        catch (CodeMismatchException)
        {
            throw new AuthException("Código de confirmação inválido.");
        }
        catch (ExpiredCodeException)
        {
            throw new AuthException("Código de confirmação expirado. Solicite um novo.");
        }
        catch (AmazonCognitoIdentityProviderException ex)
        {
            throw new AuthException(ex.Message);
        }
    }

    public async Task ResendConfirmationCodeAsync(string email, CancellationToken cancellationToken)
    {
        var request = new ResendConfirmationCodeRequest
        {
            ClientId = _options.ClientId,
            Username = email,
            SecretHash = ComputeSecretHash(email),
        };

        try
        {
            await cognito.ResendConfirmationCodeAsync(request, cancellationToken);
        }
        catch (AmazonCognitoIdentityProviderException ex)
        {
            throw new AuthException(ex.Message);
        }
    }

    public async Task<CognitoAuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var request = new InitiateAuthRequest
        {
            ClientId = _options.ClientId,
            AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
            AuthParameters = new Dictionary<string, string>
            {
                ["USERNAME"] = email,
                ["PASSWORD"] = password,
            },
        };

        var secretHash = ComputeSecretHash(email);
        if (secretHash is not null)
        {
            request.AuthParameters["SECRET_HASH"] = secretHash;
        }

        try
        {
            var response = await cognito.InitiateAuthAsync(request, cancellationToken);

            if (response.AuthenticationResult is null)
            {
                throw new AuthException($"Etapa adicional de login necessária: {response.ChallengeName}.");
            }

            return new CognitoAuthResult(
                response.AuthenticationResult.AccessToken,
                response.AuthenticationResult.IdToken,
                response.AuthenticationResult.RefreshToken);
        }
        catch (UserNotConfirmedException)
        {
            throw new AuthException("Confirme seu e-mail antes de entrar.");
        }
        catch (NotAuthorizedException)
        {
            throw new AuthException("E-mail ou senha inválidos.");
        }
        catch (UserNotFoundException)
        {
            throw new AuthException("E-mail ou senha inválidos.");
        }
        catch (AmazonCognitoIdentityProviderException ex)
        {
            throw new AuthException(ex.Message);
        }
    }

    private string? ComputeSecretHash(string username)
    {
        if (string.IsNullOrEmpty(_options.ClientSecret))
        {
            return null;
        }

        var key = Encoding.UTF8.GetBytes(_options.ClientSecret);
        var message = Encoding.UTF8.GetBytes(username + _options.ClientId);
        using var hmac = new HMACSHA256(key);
        return Convert.ToBase64String(hmac.ComputeHash(message));
    }
}
