namespace CompenseAgora.Common.Exceptions;

/// <summary>User-facing authentication/registration failure (invalid credentials, duplicate email, bad code, etc.).</summary>
public class AuthException(string message) : Exception(message);
