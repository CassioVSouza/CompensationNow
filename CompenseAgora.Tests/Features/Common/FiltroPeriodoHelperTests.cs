using CompenseAgora.Features.Common;

namespace CompenseAgora.Tests.Features.Common;

public class FiltroPeriodoHelperTests
{
    [Fact]
    public void CalcularIntervalo_Mensal_ReturnsFirstAndLastDayOfGivenMonth()
    {
        var (inicio, fim) = FiltroPeriodoHelper.CalcularIntervalo(TipoPeriodoFiltro.Mensal, mes: 2, ano: 2026, dataInicio: null, dataFim: null);

        Assert.Equal(new DateOnly(2026, 2, 1), inicio);
        Assert.Equal(new DateOnly(2026, 2, 28), fim);
    }

    [Fact]
    public void CalcularIntervalo_Mensal_HandlesLeapYearFebruary()
    {
        var (inicio, fim) = FiltroPeriodoHelper.CalcularIntervalo(TipoPeriodoFiltro.Mensal, mes: 2, ano: 2024, dataInicio: null, dataFim: null);

        Assert.Equal(new DateOnly(2024, 2, 1), inicio);
        Assert.Equal(new DateOnly(2024, 2, 29), fim);
    }

    [Theory]
    [InlineData(null, 2026)]
    [InlineData(0, 2026)]
    [InlineData(13, 2026)]
    public void CalcularIntervalo_Mensal_Throws_WhenMesMissingOrOutOfRange(int? mes, int ano)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            FiltroPeriodoHelper.CalcularIntervalo(TipoPeriodoFiltro.Mensal, mes, ano, null, null));

        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.Contains("mês", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CalcularIntervalo_Mensal_Throws_WhenAnoMissing()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            FiltroPeriodoHelper.CalcularIntervalo(TipoPeriodoFiltro.Mensal, mes: 5, ano: null, dataInicio: null, dataFim: null));

        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.Contains("ano", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CalcularIntervalo_Personalizado_ReturnsGivenDatesUnchanged()
    {
        var dataInicio = new DateOnly(2026, 3, 10);
        var dataFim = new DateOnly(2026, 5, 20);

        var (inicio, fim) = FiltroPeriodoHelper.CalcularIntervalo(TipoPeriodoFiltro.Personalizado, null, null, dataInicio, dataFim);

        Assert.Equal(dataInicio, inicio);
        Assert.Equal(dataFim, fim);
    }

    [Fact]
    public void CalcularIntervalo_Personalizado_ReturnsSameDay_WhenInicioEqualsFim()
    {
        var data = new DateOnly(2026, 3, 10);

        var (inicio, fim) = FiltroPeriodoHelper.CalcularIntervalo(TipoPeriodoFiltro.Personalizado, null, null, data, data);

        Assert.Equal(data, inicio);
        Assert.Equal(data, fim);
    }

    [Fact]
    public void CalcularIntervalo_Personalizado_Throws_WhenDataInicioIsNull()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            FiltroPeriodoHelper.CalcularIntervalo(TipoPeriodoFiltro.Personalizado, null, null, null, new DateOnly(2026, 1, 1)));

        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public void CalcularIntervalo_Personalizado_Throws_WhenDataFimIsNull()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            FiltroPeriodoHelper.CalcularIntervalo(TipoPeriodoFiltro.Personalizado, null, null, new DateOnly(2026, 1, 1), null));

        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public void CalcularIntervalo_Personalizado_Throws_WhenDataInicioIsAfterDataFim()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            FiltroPeriodoHelper.CalcularIntervalo(
                TipoPeriodoFiltro.Personalizado, null, null, new DateOnly(2026, 5, 20), new DateOnly(2026, 3, 10)));

        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.Contains("início", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CalcularIntervalo_Anual_ReturnsJanuaryFirstToDecemberThirtyFirst()
    {
        var (inicio, fim) = FiltroPeriodoHelper.CalcularIntervalo(TipoPeriodoFiltro.Anual, null, ano: 2026, dataInicio: null, dataFim: null);

        Assert.Equal(new DateOnly(2026, 1, 1), inicio);
        Assert.Equal(new DateOnly(2026, 12, 31), fim);
    }

    [Fact]
    public void CalcularIntervalo_Anual_Throws_WhenAnoMissing()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            FiltroPeriodoHelper.CalcularIntervalo(TipoPeriodoFiltro.Anual, null, null, null, null));

        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.Contains("ano", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CalcularIntervalo_Ultimos12Meses_ReturnsElevenMonthsAgoStartThroughCurrentMonthEnd()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var mesAtualInicio = new DateOnly(hoje.Year, hoje.Month, 1);
        var esperadoInicio = mesAtualInicio.AddMonths(-11);
        var esperadoFim = mesAtualInicio.AddMonths(1).AddDays(-1);

        var (inicio, fim) = FiltroPeriodoHelper.CalcularIntervalo(TipoPeriodoFiltro.Ultimos12Meses, null, null, null, null);

        Assert.Equal(esperadoInicio, inicio);
        Assert.Equal(esperadoFim, fim);
    }

    [Fact]
    public void CalcularIntervalo_Ultimos12Meses_IgnoresOtherParameters()
    {
        var (semParametros, _) = (FiltroPeriodoHelper.CalcularIntervalo(TipoPeriodoFiltro.Ultimos12Meses, null, null, null, null).Inicio, 0);
        var (comParametros, _) = (FiltroPeriodoHelper.CalcularIntervalo(
            TipoPeriodoFiltro.Ultimos12Meses, 7, 1999, new DateOnly(2000, 1, 1), new DateOnly(2000, 1, 2)).Inicio, 0);

        Assert.Equal(semParametros, comParametros);
    }
}
