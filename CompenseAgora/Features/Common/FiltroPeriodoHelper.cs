namespace CompenseAgora.Features.Common;

public static class FiltroPeriodoHelper
{
    public static (DateOnly Inicio, DateOnly Fim) CalcularIntervalo(
        TipoPeriodoFiltro tipo,
        int? mes,
        int? ano,
        DateOnly? dataInicio,
        DateOnly? dataFim)
    {
        switch (tipo)
        {
            case TipoPeriodoFiltro.Mensal:
                if (mes is null || mes < 1 || mes > 12)
                    throw new ArgumentException("Informe um mês válido (1-12) para o filtro mensal.");
                if (ano is null)
                    throw new ArgumentException("Informe o ano para o filtro mensal.");

                var inicioMensal = new DateOnly(ano.Value, mes.Value, 1);
                var fimMensal = inicioMensal.AddMonths(1).AddDays(-1);
                return (inicioMensal, fimMensal);

            case TipoPeriodoFiltro.Personalizado:
                if (dataInicio is null || dataFim is null)
                    throw new ArgumentException("Informe a data de início e a data de fim para o filtro personalizado.");
                if (dataInicio > dataFim)
                    throw new ArgumentException("A data de início não pode ser posterior à data de fim.");

                return (dataInicio.Value, dataFim.Value);

            case TipoPeriodoFiltro.Ultimos12Meses:
                var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
                var mesAtualInicio = new DateOnly(hoje.Year, hoje.Month, 1);
                var inicioUltimos12 = mesAtualInicio.AddMonths(-11);
                var fimUltimos12 = mesAtualInicio.AddMonths(1).AddDays(-1);
                return (inicioUltimos12, fimUltimos12);

            case TipoPeriodoFiltro.Anual:
                if (ano is null)
                    throw new ArgumentException("Informe o ano para o filtro anual.");

                var inicioAnual = new DateOnly(ano.Value, 1, 1);
                var fimAnual = new DateOnly(ano.Value, 12, 31);
                return (inicioAnual, fimAnual);

            default:
                throw new ArgumentException("Tipo de período de filtro inválido.");
        }
    }
}
