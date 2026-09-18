using CompenseAgora.Data;
using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Features.Energias.Calculo;

public interface ICalculadoraEmissaoEnergia
{
    Task<decimal> CalcularEmissaoCO2Async(
        DateOnly dataReferencia,
        decimal quantidade,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Calcula a emissão de CO2e de um registro de energia. Adaptado do EnergiaService do projeto
/// NeutralizaProjectMVC (ver Calculation.txt): Emissão = Quantidade consumida (kWh) × fator de emissão do
/// SIN (FeSin, kg CO2e/kWh) do mês de referência, o mesmo "fator médio mensal de emissão de CO2 do Sistema
/// Interligado Nacional" publicado pelo MCTI/ONS.
/// O original também tinha um modo "anual" (que somava, em vez de calcular a média, os 12 fatores mensais
/// do ano — o próprio fallback do código original usava média, então a soma no caminho principal parece um
/// bug daquela versão) selecionado por um campo de tipo de referência que não existe no formulário desta
/// base (Energia.DataReferencia é sempre um mês específico, como em Viagem). Por isso aqui só existe o modo
/// mensal, com o mesmo tipo de busca por (Ano, Mês) e fallback usado em CalculadoraEmissaoViagem: ano/mês
/// exato, senão o fator disponível mais recente igual ou anterior à data de referência, senão o mais antigo.
/// Fator ausente é tratado como zero em vez de lançar exceção, já que FATOR_ENERGIA ainda não tem tela de
/// cadastro nesta base.
/// </summary>
public class CalculadoraEmissaoEnergia(CompenseAgoraDbContext dbContext) : ICalculadoraEmissaoEnergia
{
    public async Task<decimal> CalcularEmissaoCO2Async(
        DateOnly dataReferencia, decimal quantidade, CancellationToken cancellationToken = default)
    {
        if (quantidade <= 0)
        {
            return 0m;
        }

        var fator = await ObterFatorEnergiaAsync(dataReferencia, cancellationToken);
        if (fator is null)
        {
            return 0m;
        }

        return Math.Round(quantidade * fator.FeSin, 6, MidpointRounding.AwayFromZero);
    }

    private async Task<FatorEnergia?> ObterFatorEnergiaAsync(DateOnly dataReferencia, CancellationToken cancellationToken)
    {
        var exato = await dbContext.FatoresEnergia
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Ano == dataReferencia.Year && f.Mes == dataReferencia.Month, cancellationToken);
        if (exato is not null)
        {
            return exato;
        }

        var disponiveis = await dbContext.FatoresEnergia
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
}
