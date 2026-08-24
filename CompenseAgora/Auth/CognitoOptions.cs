namespace CompenseAgora.Auth;

public class CognitoOptions
{
    public const string SectionName = "Cognito";

    public string UserPoolId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Only set when the Cognito App Client was created with a client secret (confidential client).</summary>
    public string? ClientSecret { get; set; }
}
