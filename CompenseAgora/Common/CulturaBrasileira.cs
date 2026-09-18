using System.Globalization;

namespace CompenseAgora.Common;

/// <summary>
/// MudBlazor's numeric inputs (<c>MudNumericField&lt;T&gt;</c>) default their own <c>Culture</c>
/// parameter to en-US regardless of the app's ambient <see cref="CultureInfo.CurrentCulture"/> (set
/// pt-BR app-wide in Program.cs) — so every numeric field must be given this culture explicitly via
/// <c>Culture="@CulturaBrasileira.PtBr"</c>, or it silently accepts "." and treats "," as a thousands
/// separator instead of the decimal separator Brazilian users expect.
/// </summary>
public static class CulturaBrasileira
{
    public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
}
