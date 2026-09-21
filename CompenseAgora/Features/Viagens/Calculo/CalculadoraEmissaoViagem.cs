using CompenseAgora.Data;
using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Viagens.Calculo;

public interface ICalculadoraEmissaoViagem
{
    Task<decimal> CalcularEmissaoCO2Async(
        int? codigoFrota,
        int? codigoCombustivel,
        DateOnly dataReferencia,
        decimal consumo,
        int anoFrota,
        decimal distanciaKm,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Calcula a emissão de CO2e (CO2 + CH4 + N2O, convertidos por GWP) de uma viagem por transporte rodoviário.
/// Fórmulas conferidas linha a linha contra a planilha oficial "Ferramenta de Cálculo GHG Protocol Brasil",
/// aba "Transp.&amp; Distribuição (Upstream)", seção Transporte Rodoviário — que usa fórmulas diferentes
/// para cada uma das três formas de registrar a viagem (Opção 1/2/3 na planilha):
/// - "Por combustível" (Opção 2 — um <see cref="Entities.Combustivel"/> específico escolhido na viagem):
///   não depende de nenhum veículo/frota. CO2, CH4 e N2O vêm todos do fator do PRÓPRIO combustível
///   escolhido (tabela FATORES_DO_COMBUSTIVEL), cada gás dividido entre a parcela fóssil e biogênica
///   conforme o par fóssil/biogênico configurado nesse combustível
///   (<see cref="Combustivel.CombustivelFossil"/>/<see cref="Combustivel.CombustivelBiogenico"/>). Aqui,
///   fora dos combustíveis com mistura rastreada (gasolina/diesel), a planilha NÃO zera a parcela — repassa
///   o consumo integralmente como fóssil (e como biogênico também, se houver um biogênico configurado).
/// - "Por tipo e ano" e "por distância" (Opções 1 e 3 — sem combustível específico escolhido; o consumo é
///   informado direto ou derivado da distância via consumo médio do veículo): dependem de um veículo
///   (<see cref="Frota"/>). O CO2 vem do fator do combustível fóssil configurado no veículo, mas o CH4 e o
///   N2O vêm de uma tabela por veículo/ano (FATOR_EMISSAO_FROTA aqui) aplicada sobre o consumo total
///   (fóssil + biogênico somados, sem dividir por fator de cada combustível). Aqui a divisão fóssil/biogênica
///   é calculada de forma independente para cada lado, cada uma com sua própria árvore de três ramos (ver
///   <see cref="CalcularParcelaFossilFrotaAsync"/>/<see cref="CalcularParcelaBiogenicaFrotaAsync"/>) — e,
///   diferente do modo "por combustível", fora dos nomes reconhecidos a parcela é ZERADA, não repassada
///   integralmente. Essa diferença de comportamento entre os dois modos é da própria planilha oficial, não
///   uma simplificação: as fórmulas de cada opção usam ramos "senão" distintos (<c>IF(...,valor)</c> na
///   Opção 2 contra <c>IF(...,0)</c> nas Opções 1/3).
/// Qualquer referência ausente (veículo, combustível, fatores, consumo médio, GWP) é tratada como
/// zero/neutra em vez de lançar exceção, já que as tabelas de fatores ainda não têm uma tela de cadastro
/// nesta base.
/// </summary>
public class CalculadoraEmissaoViagem(CompenseAgoraDbContext dbContext) : ICalculadoraEmissaoViagem
{
    private const string NomeGasCO2 = "Dióxido de carbono (CO2)";
    private const string NomeGasCH4 = "Metano (CH4)";
    private const string NomeGasN2O = "Óxido nitroso (N2O)";
    private const string NomeFrotaGLP = "Gás Liquefeito de Petróleo (GLP)";
    private const string NomeFrotaGasolinaPadrao = "Automóvel a gasolina";

    private const string NomeGasolinaAutomotivaPura = "Gasolina Automotiva (pura)";
    private const string NomeOleoDieselPuro = "Óleo Diesel (puro)";
    private const string NomeGasNaturalVeicular = "Gás Natural Veicular (GNV)";
    private const string NomeEtanolAnidro = "Etanol Anidro";
    private const string NomeBiodiesel = "Biodiesel (B100)";
    private const string NomeEtanolHidratado = "Etanol Hidratado";
    private const string NomeBiometano = "Biometano";

    private static readonly string[] NomesFosseisComMisturaRastreada =
    [
        NomeGasolinaAutomotivaPura,
        NomeOleoDieselPuro,
        "Óleo Diesel (comercial)",
    ];

    public async Task<decimal> CalcularEmissaoCO2Async(
        int? codigoFrota,
        int? codigoCombustivel,
        DateOnly dataReferencia,
        decimal consumo,
        int anoFrota,
        decimal distanciaKm,
        CancellationToken cancellationToken = default)
    {
        decimal co2, ch4, n2o;

        if (codigoCombustivel is not null)
        {
            if (consumo <= 0)
            {
                return 0m;
            }

            (co2, ch4, n2o) = await CalcularPorCombustivelAsync(codigoCombustivel.Value, dataReferencia, consumo, cancellationToken);
        }
        else
        {
            if (codigoFrota is null)
            {
                return 0m;
            }

            var frota = await dbContext.Frotas
                .AsNoTracking()
                .Include(f => f.CombustivelBiogenico)
                .Include(f => f.CombustivelFossil)
                .Include(f => f.CombustivelPrimario)
                .FirstOrDefaultAsync(f => f.Codigo == codigoFrota.Value, cancellationToken);

            if (frota is null)
            {
                return 0m;
            }

            var consumoEfetivo = await DeterminarConsumoEfetivoAsync(frota, consumo, anoFrota, distanciaKm, cancellationToken);
            if (consumoEfetivo <= 0)
            {
                return 0m;
            }

            (co2, ch4, n2o) = await CalcularPorFrotaAsync(frota, dataReferencia, consumoEfetivo, anoFrota, cancellationToken);
        }

        var gwp = await ObterGwpAsync(cancellationToken);
        var totalCO2e = co2 * gwp[NomeGasCO2] + ch4 * gwp[NomeGasCH4] + n2o * gwp[NomeGasN2O];

        return Math.Round(totalCO2e, 6, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// "Por combustível": não depende de veículo. CO2, CH4 e N2O vêm do fator do próprio combustível
    /// escolhido na viagem (fóssil e biogênico configurados nele), cada um aplicado à sua fração do
    /// consumo.
    /// </summary>
    private async Task<(decimal CO2, decimal CH4, decimal N2O)> CalcularPorCombustivelAsync(
        int codigoCombustivel, DateOnly dataReferencia, decimal consumoEfetivo, CancellationToken cancellationToken)
    {
        var combustivel = await dbContext.Combustiveis
            .AsNoTracking()
            .Include(c => c.CombustivelFossil)
            .Include(c => c.CombustivelBiogenico)
            .FirstOrDefaultAsync(c => c.Codigo == codigoCombustivel, cancellationToken);

        if (combustivel is null)
        {
            return (0m, 0m, 0m);
        }

        var (consumoFossil, consumoBiogenico) = await DividirConsumoPorCombustivelAsync(
            combustivel.CodigoCombustivelFossil, combustivel.CombustivelFossil?.Nome,
            combustivel.CodigoCombustivelBiogenico, combustivel.CombustivelBiogenico?.Nome,
            dataReferencia, consumoEfetivo, cancellationToken);

        var fatorFossil = await ObterFatorCombustivelAsync(combustivel.CodigoCombustivelFossil, dataReferencia, cancellationToken);
        var fatorBiogenico = await ObterFatorCombustivelAsync(combustivel.CodigoCombustivelBiogenico, dataReferencia, cancellationToken);

        var co2 = fatorFossil.CO2 * consumoFossil / 1000m;
        var ch4 = (fatorFossil.CH4 * consumoFossil + fatorBiogenico.CH4 * consumoBiogenico) / 1000m;
        var n2o = (fatorFossil.N2O * consumoFossil + fatorBiogenico.N2O * consumoBiogenico) / 1000m;

        return (co2, ch4, n2o);
    }

    /// <summary>
    /// "Por tipo e ano" / "por distância": CO2 continua vindo do fator do combustível fóssil do veículo,
    /// mas CH4 e N2O vêm do fator de emissão do próprio veículo (por veículo/ano), aplicado ao consumo
    /// total (fóssil + biogênico somados) em vez de dividido por combustível.
    /// </summary>
    private async Task<(decimal CO2, decimal CH4, decimal N2O)> CalcularPorFrotaAsync(
        Frota frota, DateOnly dataReferencia, decimal consumoEfetivo, int anoFrota, CancellationToken cancellationToken)
    {
        // O fóssil "efetivo" da frota é o par explícito (CombustivelFossil), ou, quando não configurado,
        // o próprio combustível primário — mesma regra usada abaixo para o fator de CO2.
        var nomeCombustivelFossilEfetivo = frota.CombustivelFossil?.Nome ?? frota.CombustivelPrimario.Nome;

        var (consumoFossil, consumoBiogenico) = await DividirConsumoPorFrotaAsync(
            nomeCombustivelFossilEfetivo, frota.CombustivelBiogenico?.Nome,
            dataReferencia, consumoEfetivo, cancellationToken);

        var codigoCombustivelFossil = frota.CodigoCombustivelFossil ?? frota.CodigoCombustivelPrimario;
        var fatorFossil = await ObterFatorCombustivelAsync(codigoCombustivelFossil, dataReferencia, cancellationToken);
        var fatorFrota = await ObterFatorEmissaoFrotaAsync(frota, anoFrota, cancellationToken);

        var co2 = fatorFossil.CO2 * consumoFossil / 1000m;
        // CH4/N2O usam o consumo bruto (não a soma fóssil+biogênico) — a planilha oficial soma as colunas
        // de consumo originais (SUM(V:AH)) aqui, não as parcelas já divididas, que agora podem somar menos
        // que o consumo total quando o combustível não é reconhecido em nenhum dos dois ramos.
        var ch4 = fatorFrota.CH4 * consumoEfetivo / 1000m;
        var n2o = fatorFrota.N2O * consumoEfetivo / 1000m;

        return (co2, ch4, n2o);
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
    /// "Por combustível" (planilha oficial, aba Transporte Rodoviário / opção 2): a divisão fóssil/biogênica
    /// só se aplica (percentual de mistura) quando o combustível fóssil é gasolina pura ou diesel puro; para
    /// qualquer outro fóssil (GNV, GLP, ...) TODO o consumo passa integralmente como fóssil, e — se houver um
    /// biogênico configurado mesmo assim — o consumo passa integralmente como biogênico também (a planilha
    /// não zera nenhum dos dois lados fora dos combustíveis com mistura rastreada).
    /// </summary>
    private async Task<(decimal Fossil, decimal Biogenico)> DividirConsumoPorCombustivelAsync(
        int? codigoCombustivelFossil, string? nomeCombustivelFossil,
        int? codigoCombustivelBiogenico, string? nomeCombustivelBiogenico,
        DateOnly dataReferencia, decimal consumoEfetivo, CancellationToken cancellationToken)
    {
        if (codigoCombustivelBiogenico is null || codigoCombustivelFossil is null)
        {
            return (consumoEfetivo, 0m);
        }

        if (nomeCombustivelFossil is null || !NomesFosseisComMisturaRastreada.Contains(nomeCombustivelFossil))
        {
            return (consumoEfetivo, 0m);
        }

        var percentual = await ObterPercentualMisturaAsync(nomeCombustivelBiogenico!, dataReferencia, cancellationToken);
        if (percentual <= 0)
        {
            return (consumoEfetivo, 0m);
        }

        var biogenico = consumoEfetivo * (percentual / 100m);
        var fossil = consumoEfetivo - biogenico;
        return (fossil, biogenico);
    }

    /// <summary>
    /// "Por tipo e ano" / "por distância" (planilha oficial, aba Transporte Rodoviário / opções 1 e 3): a
    /// parcela fóssil e a parcela biogênica são calculadas de forma INDEPENDENTE (não complementar), cada
    /// uma com sua própria árvore de três ramos:
    /// - Fóssil: gasolina pura ou diesel puro → consumo × (1 − % de mistura); GNV → consumo integral (sem
    ///   redução); qualquer outro fóssil (GLP, elétrico, ...) → zero.
    /// - Biogênico: etanol anidro ou biodiesel → consumo × (% de mistura); etanol hidratado ou biometano →
    ///   consumo integral; qualquer outro biogênico → zero.
    /// Ao contrário do modo "por combustível", aqui o "senão" zera a parcela em vez de repassá-la
    /// integralmente — replicando exatamente as fórmulas da planilha oficial (colunas AL/AY e BA/BN das
    /// opções 1 e 3), que usam <c>IF(...,0)</c> em vez de <c>IF(...,consumo)</c> no último ramo.
    /// </summary>
    private async Task<(decimal Fossil, decimal Biogenico)> DividirConsumoPorFrotaAsync(
        string? nomeCombustivelFossil, string? nomeCombustivelBiogenico,
        DateOnly dataReferencia, decimal consumoEfetivo, CancellationToken cancellationToken)
    {
        var fossil = await CalcularParcelaFossilFrotaAsync(nomeCombustivelFossil, dataReferencia, consumoEfetivo, cancellationToken);
        var biogenico = await CalcularParcelaBiogenicaFrotaAsync(nomeCombustivelBiogenico, dataReferencia, consumoEfetivo, cancellationToken);
        return (fossil, biogenico);
    }

    private async Task<decimal> CalcularParcelaFossilFrotaAsync(
        string? nomeCombustivelFossil, DateOnly dataReferencia, decimal consumoEfetivo, CancellationToken cancellationToken)
    {
        if (nomeCombustivelFossil is null)
        {
            return 0m;
        }

        if (nomeCombustivelFossil is NomeGasolinaAutomotivaPura or NomeOleoDieselPuro)
        {
            // A % de mistura correspondente vem sempre do lado etanol/biodiesel — usa o nome "irmão"
            // biogênico de cada fóssil só para escolher qual coluna de composição (etanol x biodiesel)
            // consultar, não para determinar se há mistura ou não (isso já foi decidido acima).
            var nomeParaEscolherComposicao = nomeCombustivelFossil == NomeGasolinaAutomotivaPura
                ? NomeEtanolAnidro
                : NomeBiodiesel;
            var percentual = await ObterPercentualMisturaAsync(nomeParaEscolherComposicao, dataReferencia, cancellationToken);
            return consumoEfetivo * (1 - percentual / 100m);
        }

        if (nomeCombustivelFossil == NomeGasNaturalVeicular)
        {
            return consumoEfetivo;
        }

        return 0m;
    }

    private async Task<decimal> CalcularParcelaBiogenicaFrotaAsync(
        string? nomeCombustivelBiogenico, DateOnly dataReferencia, decimal consumoEfetivo, CancellationToken cancellationToken)
    {
        if (nomeCombustivelBiogenico is null)
        {
            return 0m;
        }

        if (nomeCombustivelBiogenico is NomeEtanolAnidro or NomeBiodiesel)
        {
            var percentual = await ObterPercentualMisturaAsync(nomeCombustivelBiogenico, dataReferencia, cancellationToken);
            return consumoEfetivo * (percentual / 100m);
        }

        if (nomeCombustivelBiogenico is NomeEtanolHidratado or NomeBiometano)
        {
            return consumoEfetivo;
        }

        return 0m;
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

    /// <summary>
    /// Fator de emissão de CH4/N2O do próprio veículo/ano (FATOR_EMISSAO_FROTA). Replica o fallback do
    /// original: casamento exato por ano (ou uma linha "para todos os anos") primeiro; se não houver
    /// fator com CH4 diferente de zero, cai para o fator mais recente disponível do mesmo veículo — com
    /// uma exceção: veículos a GLP reaproveitam o fator do veículo "Automóvel a gasolina" nesse fallback,
    /// como no projeto original. Sem nenhum fator disponível, CH4/N2O ficam zerados.
    /// </summary>
    private async Task<FatorEmissaoFrotaValores> ObterFatorEmissaoFrotaAsync(
        Frota frota, int anoFrota, CancellationToken cancellationToken)
    {
        var exato = await dbContext.FatoresEmissaoFrota
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.CodigoFrota == frota.Codigo && (f.Ano == anoFrota || f.ParaTodos), cancellationToken);

        if (exato is not null && exato.CH4 != 0)
        {
            return new FatorEmissaoFrotaValores(exato.CH4, exato.N2O);
        }

        var codigoFrotaParaFallback = frota.Codigo;

        if (frota.Nome == NomeFrotaGLP)
        {
            var frotaPadrao = await dbContext.Frotas
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Nome == NomeFrotaGasolinaPadrao, cancellationToken);
            if (frotaPadrao is not null)
            {
                codigoFrotaParaFallback = frotaPadrao.Codigo;
            }
        }

        var disponiveis = await dbContext.FatoresEmissaoFrota
            .AsNoTracking()
            .Where(f => f.CodigoFrota == codigoFrotaParaFallback && f.CH4 != 0)
            .OrderBy(f => f.Ano)
            .ToListAsync(cancellationToken);

        if (disponiveis.Count == 0)
        {
            return FatorEmissaoFrotaValores.Zero;
        }

        var ultimo = disponiveis.Last();
        return new FatorEmissaoFrotaValores(ultimo.CH4, ultimo.N2O);
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

    private readonly record struct FatorEmissaoFrotaValores(decimal CH4, decimal N2O)
    {
        public static readonly FatorEmissaoFrotaValores Zero = new(0m, 0m);
    }
}
