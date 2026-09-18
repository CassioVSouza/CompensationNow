using System.Globalization;

namespace CompenseAgora.Common;

/// <summary>
/// Emissões já são calculadas e armazenadas em toneladas de CO2 equivalente (t CO2e) — este formatador
/// só centraliza o texto "t CO2e" exibido ao usuário, sem nenhuma conversão de unidade.
/// </summary>
public static class FormatadorEmissao
{
    public static decimal ParaToneladas(decimal valor) => valor;

    public static string FormatarToneladas(decimal valor) =>
        $"{valor.ToString("N3", CultureInfo.CurrentCulture)} t CO2e";
}
