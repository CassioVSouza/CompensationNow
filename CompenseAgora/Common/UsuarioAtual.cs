using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace CompenseAgora.Common;

/// <summary>Resolves the logged-in <c>Pessoa.Codigo</c> for code that has no component context (e.g. EF interceptors).</summary>
public interface IUsuarioAtual
{
    /// <summary>Returns the current user's <c>Pessoa.Codigo</c>, or null when nobody is signed in.</summary>
    Task<int?> ObterCodigoPessoaAsync();
}

/// <summary>
/// Prefers the Blazor <see cref="AuthenticationStateProvider"/>, which is correct both inside an
/// Interactive Server circuit (where <c>IHttpContextAccessor</c> is unreliable) and during static SSR.
/// Falls back to the HttpContext when the provider has no state yet (e.g. a non-Blazor request).
/// </summary>
public class UsuarioAtual(AuthenticationStateProvider authenticationStateProvider, IHttpContextAccessor httpContextAccessor)
    : IUsuarioAtual
{
    public async Task<int?> ObterCodigoPessoaAsync()
    {
        ClaimsPrincipal? usuario;
        try
        {
            usuario = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
        }
        catch (InvalidOperationException)
        {
            usuario = httpContextAccessor.HttpContext?.User;
        }

        return int.TryParse(usuario?.FindFirstValue(ClaimTypes.NameIdentifier), out var codigo) ? codigo : null;
    }
}
