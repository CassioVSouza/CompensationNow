using System.Text.RegularExpressions;
using CompenseAgora.Common;
using CompenseAgora.Entities;

namespace CompenseAgora.Features.Auditorias;

/// <summary>
/// Turns the raw identifiers stored in <see cref="Auditoria"/> (CLR entity/property names, which are
/// accent-free Portuguese) into the pt-BR labels shown on the Histórico page.
/// </summary>
public static partial class RotulosAuditoria
{
    private static readonly Dictionary<string, string> Entidades = new()
    {
        [nameof(Pessoa)] = "perfil",
        [nameof(GasEfeitoEstufa)] = "gás de efeito estufa",
        [nameof(Combustivel)] = "combustível",
        [nameof(FatorCombustivel)] = "fator do combustível",
        [nameof(Frota)] = "veículo",
        [nameof(ConsumoMedioFrota)] = "consumo médio do veículo",
        [nameof(Viagem)] = "viagem",
        [nameof(Energia)] = "registro de energia",
        [nameof(Compensacao)] = "compensação",
        [nameof(FatorComposicaoCombustivel)] = "composição do combustível",
        [nameof(FatorEnergia)] = "fator de energia",
        [nameof(FatorEmissaoFrota)] = "fator de emissão do veículo",
    };

    private static readonly Dictionary<string, string> Campos = new()
    {
        ["CodigoPessoa"] = "Pessoa (código)",
        ["CodigoFrota"] = "Veículo (código)",
        ["CodigoCombustivel"] = "Combustível (código)",
        ["CodigoCombustivelPrimario"] = "Combustível primário (código)",
        ["CodigoCombustivelBiogenico"] = "Combustível biogênico (código)",
        ["CodigoCombustivelFossil"] = "Combustível fóssil (código)",
        ["DistanciaKM"] = "Distância (km)",
        ["EmissaoCO2"] = "Emissão de CO2",
        ["GWP"] = "GWP",
        ["CO2"] = "CO2",
        ["CH4"] = "CH4",
        ["N2O"] = "N2O",
        ["FeSin"] = "FE SIN",
    };

    // Accent restoration for words that appear in property names; anything not listed is shown as-is.
    private static readonly Dictionary<string, string> Palavras = new(StringComparer.OrdinalIgnoreCase)
    {
        ["codigo"] = "código",
        ["combustivel"] = "combustível",
        ["biogenico"] = "biogênico",
        ["fossil"] = "fóssil",
        ["primario"] = "primário",
        ["referencia"] = "referência",
        ["emissao"] = "emissão",
        ["compensacao"] = "compensação",
        ["medio"] = "médio",
        ["distancia"] = "distância",
        ["endereco"] = "endereço",
        ["numero"] = "número",
        ["pais"] = "país",
        ["calorifico"] = "calorífico",
        ["familia"] = "família",
        ["mes"] = "mês",
        ["composicao"] = "composição",
    };

    public static string Entidade(string? entidade) =>
        entidade is not null && Entidades.TryGetValue(entidade, out var rotulo) ? rotulo : entidade ?? "registro";

    public static string Campo(string campo)
    {
        if (Campos.TryGetValue(campo, out var rotulo))
        {
            return rotulo;
        }

        var palavras = PalavrasPascalCase().Matches(campo)
            .Select(m => Palavras.GetValueOrDefault(m.Value, m.Value.ToLowerInvariant()))
            .ToList();
        if (palavras.Count == 0)
        {
            return campo;
        }

        palavras[0] = char.ToUpperInvariant(palavras[0][0]) + palavras[0][1..];
        return string.Join(' ', palavras);
    }

    public static string Acao(TipoAcaoAuditoria acao) => acao switch
    {
        TipoAcaoAuditoria.Criacao => "Criação",
        TipoAcaoAuditoria.Alteracao => "Alteração",
        TipoAcaoAuditoria.Exclusao => "Exclusão",
        TipoAcaoAuditoria.Filtro => "Filtro",
        _ => acao.ToString(),
    };

    public static string Descrever(
        TipoAcaoAuditoria acao, string? entidade, int? codigoRegistro, string? tela, DateOnly? inicio, DateOnly? fim)
    {
        if (acao == TipoAcaoAuditoria.Filtro)
        {
            var periodo = inicio is not null && fim is not null
                ? $": {inicio.Value.ToString("dd/MM/yyyy", CulturaBrasileira.PtBr)} a {fim.Value.ToString("dd/MM/yyyy", CulturaBrasileira.PtBr)}"
                : string.Empty;
            return $"Aplicou filtro de período na tela {tela}{periodo}";
        }

        var verbo = acao switch
        {
            TipoAcaoAuditoria.Criacao => "Criou",
            TipoAcaoAuditoria.Alteracao => "Alterou",
            _ => "Excluiu",
        };
        var codigo = codigoRegistro is not null ? $" nº {codigoRegistro}" : string.Empty;
        return $"{verbo} {Entidade(entidade)}{codigo}";
    }

    [GeneratedRegex("[A-Z]+(?![a-z])|[A-Z][a-z]*|[0-9]+")]
    private static partial Regex PalavrasPascalCase();
}
