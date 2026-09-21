namespace CompenseAgora.Common;

/// <summary>
/// MudBlazor's chart <c>YAxisTicks</c> (gridline spacing) defaults to a fixed value of 20 and does
/// nothing to adapt to the actual data — a chart whose values never exceed a few tenths of a tonne
/// still tops out at a flat 20 axis max. This computes a "nice" integer tick spacing (1/2/5 × a power
/// of ten) sized to the real data, so the axis top reflects the highest value actually present.
/// </summary>
public static class EscalaEixoY
{
    public static int CalcularEspacamento(double maiorValor, int metaDeLinhas = 5)
    {
        if (maiorValor <= 0)
        {
            return 1;
        }

        var passoBruto = maiorValor / metaDeLinhas;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(passoBruto)));
        var normalizado = passoBruto / magnitude;

        var passoNormalizado = normalizado switch
        {
            <= 1 => 1,
            <= 2 => 2,
            <= 5 => 5,
            _ => 10,
        };

        return Math.Max(1, (int)Math.Ceiling(passoNormalizado * magnitude));
    }
}
