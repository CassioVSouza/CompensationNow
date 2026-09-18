using CompenseAgora.Data;
using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Viagens.Calculo;

public interface ICalculadoraEmissaoViagem
{
    Task<decimal> CalcularEmissaoCO2Async(
        int codigoFrota,
        DateOnly dataReferencia,
        decimal consumo,
        int anoFrota,
        decimal distanciaKm,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Calcula a emissão de CO2e (CO2 + CH4 + N2O, convertidos por GWP) de uma viagem. Adaptado do serviço
/// equivalente do projeto NeutralizaProjectMVC (ver Calculation.txt na raiz) para o esquema desta base:
/// - As três formas de registro da tela de viagens (por distância, por combustível, por tipo e ano) convergem
///   para a mesma fórmula aqui: a única diferença de fato entre elas era como o "consumo" era obtido
///   (direto, ou derivado da distância via consumo médio da frota) — o restante do cálculo era idêntico.
/// - O projeto original tinha uma tabela separada de fatores de CH4/N2O por frota/ano
///   (FatorEmissaoFrotaEntity), que não existe no diagrama desta base; aqui CH4 e N2O também vêm de
///   FATORES_DO_COMBUSTIVEL (mesma fonte do CO2), que já traz os três gases por combustível/ano.
/// - O percentual de mistura biocombustível (etanol/gasolina ou biodiesel/diesel) é decidido pelo nome do
///   combustível biogênico configurado na frota, em vez de um indicador numérico separado que não existe
///   mais no modelo (Frota.CombustivelBiogenico/CombustivelFossil já expressam a mesma intenção).
/// Qualquer referência ausente (frota, fatores, consumo médio, GWP) é tratada como zero/neutra em vez de
/// lançar exceção, já que as tabelas de fatores ainda não têm uma tela de cadastro nesta base.
/// </summary>
public class CalculadoraEmissaoViagem(CompenseAgoraDbContext dbContext) : ICalculadoraEmissaoViagem
{
    private const string NomeGasCO2 = "Dióxido de carbono (CO2)";
    private const string NomeGasCH4 = "Metano (CH4)";
    private const string NomeGasN2O = "Óxido nitroso (N2O)";

    public async Task<decimal> CalcularEmissaoCO2Async(
        int codigoFrota,
        DateOnly dataReferencia,
        decimal consumo,
        int anoFrota,
        decimal distanciaKm,
        CancellationToken cancellationToken = default)
    {
        var frota = await dbContext.Frotas
            .AsNoTracking()
            .Include(f => f.CombustivelBiogenico)
            .FirstOrDefaultAsync(f => f.Codigo == codigoFrota, cancellationToken);

        if (frota is null)
        {
            return 0m;
        }

        var consumoEfetivo = await DeterminarConsumoEfetivoAsync(frota, consumo, anoFrota, distanciaKm, cancellationToken);
        if (consumoEfetivo <= 0)
        {
            return 0m;
        }

        var (consumoFossil, consumoBiogenico) = await DividirConsumoAsync(frota, dataReferencia, consumoEfetivo, cancellationToken);

        var codigoCombustivelFossil = frota.CodigoCombustivelFossil ?? frota.CodigoCombustivelPrimario;
        var fatorFossil = await ObterFatorCombustivelAsync(codigoCombustivelFossil, dataReferencia, cancellationToken);
        var fatorBiogenico = await ObterFatorCombustivelAsync(frota.CodigoCombustivelBiogenico, dataReferencia, cancellationToken);

        var gwp = await ObterGwpAsync(cancellationToken);

        var co2 = fatorFossil.CO2 * consumoFossil / 1000m;
        var ch4 = (fatorFossil.CH4 * consumoFossil + fatorBiogenico.CH4 * consumoBiogenico) / 1000m;
        var n2o = (fatorFossil.N2O * consumoFossil + fatorBiogenico.N2O * consumoBiogenico) / 1000m;

        var totalCO2e = co2 * gwp[NomeGasCO2] + ch4 * gwp[NomeGasCH4] + n2o * gwp[NomeGasN2O];

        return Math.Round(totalCO2e, 6, MidpointRounding.AwayFromZero);
    }

    private async Task<decimal> DeterminarConsumoEfetivoAsync(
        Frota frota, decimal consumo, int anoFrota, decimal distanciaKm, CancellationToken cancellationToken)
    {
        if (consumo > 0)
        {
            return consumo;
        }

        if (distanciaKm <= 0)
        {
            return 0m;
        }

        var consumoMedio = await ObterConsumoMedioAsync(frota.Codigo, anoFrota, cancellationToken);
        if (consumoMedio is null || consumoMedio.Value <= 0)
        {
            return 0m;
        }

        return distanciaKm / consumoMedio.Value;
    }

    private async Task<decimal?> ObterConsumoMedioAsync(int codigoFrota, int anoFrota, CancellationToken cancellationToken)
    {
        var exato = await dbContext.ConsumosMedioFrota
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CodigoFrota == codigoFrota && c.Ano == anoFrota, cancellationToken);
        if (exato is not null)
        {
            return exato.Consumo;
        }

        var paraTodos = await dbContext.ConsumosMedioFrota
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CodigoFrota == codigoFrota && c.ParaTodos, cancellationToken);
        if (paraTodos is not null)
        {
            return paraTodos.Consumo;
        }

        var disponiveis = await dbContext.ConsumosMedioFrota
            .AsNoTracking()
            .Where(c => c.CodigoFrota == codigoFrota)
            .OrderBy(c => c.Ano)
            .ToListAsync(cancellationToken);

        if (disponiveis.Count == 0)
        {
            return null;
        }

        var escolhido = disponiveis.LastOrDefault(c => c.Ano <= anoFrota) ?? disponiveis[0];
        return escolhido.Consumo;
    }

    /// <summary>
    /// Divide o consumo efetivo entre a parcela fóssil e a parcela biogênica. Sem um par fóssil/biogênico
    /// configurado na frota (ex.: GNV, GLP, diesel sem mistura obrigatória rastreada), todo o consumo é
    /// tratado como fóssil.
    /// </summary>
    private async Task<(decimal Fossil, decimal Biogenico)> DividirConsumoAsync(
        Frota frota, DateOnly dataReferencia, decimal consumoEfetivo, CancellationToken cancellationToken)
    {
        if (frota.CodigoCombustivelBiogenico is null || frota.CodigoCombustivelFossil is null)
        {
            return (consumoEfetivo, 0m);
        }

        var percentual = await ObterPercentualMisturaAsync(frota.CombustivelBiogenico!.Nome, dataReferencia, cancellationToken);
        if (percentual <= 0)
        {
            return (consumoEfetivo, 0m);
        }

        var biogenico = consumoEfetivo * (percentual / 100m);
        var fossil = consumoEfetivo - biogenico;
        return (fossil, biogenico);
    }

    private async Task<decimal> ObterPercentualMisturaAsync(
        string nomeCombustivelBiogenico, DateOnly dataReferencia, CancellationToken cancellationToken)
    {
        var composicao = await ObterComposicaoAsync(dataReferencia, cancellationToken);
        if (composicao is null)
        {
            return 0m;
        }

        if (nomeCombustivelBiogenico.Contains("Biodiesel", StringComparison.OrdinalIgnoreCase))
        {
            return composicao.PercentualBiodiesel;
        }

        if (nomeCombustivelBiogenico.Contains("Etanol", StringComparison.OrdinalIgnoreCase))
        {
            return composicao.PercentualEtanol;
        }

        return 0m;
    }

    private async Task<FatorComposicaoCombustivel?> ObterComposicaoAsync(DateOnly dataReferencia, CancellationToken cancellationToken)
    {
        var exata = await dbContext.FatoresComposicaoCombustivel
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Ano == dataReferencia.Year && f.Mes == dataReferencia.Month, cancellationToken);
        if (exata is not null)
        {
            return exata;
        }

        var disponiveis = await dbContext.FatoresComposicaoCombustivel
            .AsNoTracking()
            .OrderBy(f => f.Ano).ThenBy(f => f.Mes)
            .ToListAsync(cancellationToken);

        if (disponiveis.Count == 0)
        {
            return null;
        }

        var escolhido = disponiveis.LastOrDefault(f =>
            f.Ano < dataReferencia.Year || (f.Ano == dataReferencia.Year && f.Mes <= dataReferencia.Month));
        return escolhido ?? disponiveis[0];
    }

    private async Task<FatorCombustivelValores> ObterFatorCombustivelAsync(
        int? codigoCombustivel, DateOnly dataReferencia, CancellationToken cancellationToken)
    {
        if (codigoCombustivel is null)
        {
            return FatorCombustivelValores.Zero;
        }

        var exato = await dbContext.FatoresDoCombustivel
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.CodigoCombustivel == codigoCombustivel && f.Ano == dataReferencia.Year, cancellationToken);
        if (exato is not null)
        {
            return new FatorCombustivelValores(exato.CO2, exato.CH4, exato.N2O);
        }

        var disponiveis = await dbContext.FatoresDoCombustivel
            .AsNoTracking()
            .Where(f => f.CodigoCombustivel == codigoCombustivel)
            .OrderBy(f => f.Ano)
            .ToListAsync(cancellationToken);

        if (disponiveis.Count == 0)
        {
            return FatorCombustivelValores.Zero;
        }

        var escolhido = disponiveis.LastOrDefault(f => f.Ano <= dataReferencia.Year) ?? disponiveis[0];
        return new FatorCombustivelValores(escolhido.CO2, escolhido.CH4, escolhido.N2O);
    }

    private async Task<Dictionary<string, decimal>> ObterGwpAsync(CancellationToken cancellationToken)
    {
        var gases = await dbContext.GasesEfeitoEstufa.AsNoTracking().ToListAsync(cancellationToken);

        return new Dictionary<string, decimal>
        {
            [NomeGasCO2] = gases.FirstOrDefault(g => g.Nome == NomeGasCO2)?.GWP ?? 1m,
            [NomeGasCH4] = gases.FirstOrDefault(g => g.Nome == NomeGasCH4)?.GWP ?? 1m,
            [NomeGasN2O] = gases.FirstOrDefault(g => g.Nome == NomeGasN2O)?.GWP ?? 1m,
        };
    }

    private readonly record struct FatorCombustivelValores(decimal CO2, decimal CH4, decimal N2O)
    {
        public static readonly FatorCombustivelValores Zero = new(0m, 0m, 0m);
    }
}
