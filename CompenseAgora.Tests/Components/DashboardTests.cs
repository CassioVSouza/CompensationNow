using System.Globalization;
using System.Security.Claims;
using Bunit;
using CompenseAgora.Components.Pages.Dashboard;
using CompenseAgora.Features.Dashboard;
using CompenseAgora.Features.Dashboard.Queries.GetDashboard;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;

namespace CompenseAgora.Tests.Components;

/// <summary>
/// bUnit component tests for Components/Pages/Dashboard.razor. This is an Interactive Server
/// component (not static SSR), so - unlike the Account pages - it can be rendered directly with
/// bUnit's TestContext/BunitContext. IMediator is mocked (Moq, already used elsewhere in this suite)
/// so these tests don't touch the real Dashboard query handler or a database.
///
/// Every test ends with an explicit <c>await DisposeAsync()</c>: MudBlazor 9.10.0's internal
/// PointerEventsNoneService (resolved whenever a MudSelect/popover-based component renders - here via
/// FiltroPeriodo) implements only IAsyncDisposable, not IDisposable. bUnit's BunitContext tears its DI
/// container down synchronously in the framework-invoked Dispose(), which then throws "type only
/// implements IAsyncDisposable. Use DisposeAsync to dispose the container." even though the test body
/// itself passed. Disposing asynchronously ourselves first marks the context as already-disposed, so
/// the framework's later synchronous Dispose() becomes a no-op instead of crashing.
/// </summary>
public class DashboardTests : BunitContext
{
    private static DashboardDto BuildDto(
        decimal totalViagem, decimal totalEnergia, Action<List<MonthlyEmissionDto>>? configureMonths = null, decimal totalCompensacao = 0m)
    {
        var months = Enumerable.Range(0, 12)
            .Select(i => new MonthlyEmissionDto(2026, i + 1, 0m, 0m, 0m))
            .ToList();
        configureMonths?.Invoke(months);

        return new DashboardDto(months, totalViagem + totalEnergia, totalViagem, totalEnergia, totalCompensacao);
    }

    private Mock<IMediator> SetUpMediator(DashboardDto? dto, TaskCompletionSource<DashboardDto>? pending = null)
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDashboardQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => pending is not null ? pending.Task : Task.FromResult(dto!));
        Services.AddSingleton(mediatorMock.Object);

        var authContext = this.AddAuthorization();
        authContext.SetAuthorized("Ana Silva");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, "1"));

        return mediatorMock;
    }

    [Fact]
    public async Task Renders_LoadingSpinner_BeforeQueryResolves()
    {
        var pending = new TaskCompletionSource<DashboardDto>();
        SetUpMediator(dto: null, pending: pending);

        var cut = Render<Dashboard>();

        Assert.Contains("Carregando seu painel", cut.Markup);
        Assert.NotEmpty(cut.FindComponents<MudProgressCircular>());

        // Never resolved: no stat tiles or empty-state message should be present yet.
        Assert.DoesNotContain("Nenhuma emissão registrada ainda", cut.Markup);
        Assert.DoesNotContain("Emissão total", cut.Markup);

        await DisposeAsync();
    }

    [Fact]
    public async Task Renders_EmptyState_WhenTotalEmissaoIsZero()
    {
        var dto = BuildDto(0m, 0m);
        SetUpMediator(dto);

        var cut = Render<Dashboard>();

        Assert.Contains("Nenhuma emissão registrada ainda", cut.Markup);
        Assert.Contains("/viagens/nova", cut.Markup);
        Assert.Contains("/energias/nova", cut.Markup);

        // No stat tiles or charts in the empty state.
        Assert.Empty(cut.FindComponents<MudChart<double>>());

        await DisposeAsync();
    }

    [Fact]
    public async Task Renders_Normally_WhenNoEmissionsButHasCompensacao()
    {
        var dto = BuildDto(0m, 0m, totalCompensacao: 15m, configureMonths: months =>
        {
            months[6] = months[6] with { Compensacao = 15m };
        });
        SetUpMediator(dto);

        var cut = Render<Dashboard>();

        Assert.DoesNotContain("Nenhuma emissão registrada ainda", cut.Markup);
        Assert.Contains("Emissão total", cut.Markup);
        Assert.NotEmpty(cut.FindComponents<MudChart<double>>());

        await DisposeAsync();
    }

    [Fact]
    public async Task Renders_StatTiles_WithCorrectlyFormattedValues_WhenPopulated()
    {
        var dto = BuildDto(totalViagem: 123.456m, totalEnergia: 78.9m, months =>
        {
            months[11] = months[11] with { ViagemEmissao = 123.456m, EnergiaEmissao = 78.9m };
        });
        SetUpMediator(dto);

        var cut = Render<Dashboard>();

        Assert.Contains("Emissão total", cut.Markup);
        Assert.Contains("Emissão de viagens", cut.Markup);
        Assert.Contains("Emissão de energia", cut.Markup);

        var expectedTotal = (123.456m + 78.9m).ToString("N3", CultureInfo.CurrentCulture) + " t CO2e";
        var expectedViagem = 123.456m.ToString("N3", CultureInfo.CurrentCulture) + " t CO2e";
        var expectedEnergia = 78.9m.ToString("N3", CultureInfo.CurrentCulture) + " t CO2e";

        Assert.Contains(expectedTotal, cut.Markup);
        Assert.Contains(expectedViagem, cut.Markup);
        Assert.Contains(expectedEnergia, cut.Markup);

        await DisposeAsync();
    }

    [Fact]
    public async Task ChartOptions_YAxisTicksAreScaledToActualData_NotTheOldFixedDefault()
    {
        // Emissions well under 1 t CO2e: with MudBlazor's raw default (YAxisTicks = 20) the axis would
        // always top out at a flat 20 regardless of these values.
        var dto = BuildDto(totalViagem: 0.05m, totalEnergia: 0.02m, configureMonths: months =>
        {
            months[11] = months[11] with { ViagemEmissao = 0.05m, EnergiaEmissao = 0.02m };
        });
        SetUpMediator(dto);

        var cut = Render<Dashboard>();

        var totalChart = cut.FindComponents<MudChart<double>>()
            .Single(c => c.Instance.ChartType == ChartType.Line && c.Instance.ChartSeries[0].Name == "Emissão total");
        var options = Assert.IsType<LineChartOptions>(totalChart.Instance.ChartOptions);

        Assert.NotEqual(20, options.YAxisTicks);
        Assert.Equal(CompenseAgora.Common.EscalaEixoY.CalcularEspacamento(0.07), options.YAxisTicks);

        await DisposeAsync();
    }

    [Fact]
    public async Task Renders_AllFiveCharts_WithMonthLabelsAndCorrectSeriesCounts_WhenPopulated()
    {
        var dto = BuildDto(totalViagem: 50m, totalEnergia: 30m, totalCompensacao: 20m, configureMonths: months =>
        {
            months[0] = months[0] with { ViagemEmissao = 50m };
            months[6] = months[6] with { EnergiaEmissao = 30m, Compensacao = 20m };
        });
        SetUpMediator(dto);

        var cut = Render<Dashboard>();

        var charts = cut.FindComponents<MudChart<double>>();
        Assert.Equal(5, charts.Count);

        var lineCharts = charts.Where(c => c.Instance.ChartType == ChartType.Line).ToList();
        var barChart = charts.Single(c => c.Instance.ChartType == ChartType.StackedBar);
        Assert.Equal(4, lineCharts.Count);

        // Total-trend line chart: a single "Total emissions" series.
        var totalChart = lineCharts.Single(c => c.Instance.ChartSeries[0].Name == "Emissão total");
        Assert.Single(totalChart.Instance.ChartSeries);
        Assert.Equal(12, totalChart.Instance.ChartSeries[0].Data.Count);

        // Standalone Viagens line chart.
        var viagemChart = lineCharts.Single(c => c.Instance.ChartSeries[0].Name == "Viagens");
        Assert.Single(viagemChart.Instance.ChartSeries);

        // Standalone Energia line chart.
        var energiaChart = lineCharts.Single(c => c.Instance.ChartSeries[0].Name == "Energia");
        Assert.Single(energiaChart.Instance.ChartSeries);

        // Emitido-vs-compensado line chart: two series, Emitido and Compensado.
        var emitidoCompensadoChart = lineCharts.Single(c => c.Instance.ChartSeries[0].Name == "Emitido");
        Assert.Equal(["Emitido", "Compensado"], emitidoCompensadoChart.Instance.ChartSeries.Select(s => s.Name).ToArray());

        // Travel-vs-energy stacked bar chart: two series, Viagens and Energia.
        Assert.Equal(2, barChart.Instance.ChartSeries.Count);
        Assert.Equal(["Viagens", "Energia"], barChart.Instance.ChartSeries.Select(s => s.Name).ToArray());

        // All charts share the same 12 "MMM/yy" month labels, oldest first.
        var expectedLabels = dto.MonthlyEmissions
            .Select(m => new DateTime(m.Year, m.Month, 1).ToString("MMM/yy", CultureInfo.InvariantCulture))
            .ToArray();
        Assert.All(charts, c => Assert.Equal(expectedLabels, c.Instance.ChartLabels));

        await DisposeAsync();
    }

    [Fact]
    public async Task ReFiltering_SendsNewQueryWithAppliedDates_AndShowsSpinnerAgainWhileReloading()
    {
        var initialDto = BuildDto(10m, 5m);
        var pending = new TaskCompletionSource<DashboardDto>();
        var mediatorMock = SetUpMediator(dto: initialDto);

        // First call (on init) resolves immediately with the default window; the next call (re-filter)
        // is held pending so we can assert the spinner reappears before it resolves.
        var callCount = 0;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDashboardQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                return callCount == 1 ? Task.FromResult(initialDto) : pending.Task;
            });

        var cut = Render<Dashboard>();
        Assert.Contains("Emissão total", cut.Markup);

        var inicio = new DateOnly(2026, 1, 1);
        var fim = new DateOnly(2026, 3, 31);

        var filtro = cut.FindComponent<CompenseAgora.Components.Shared.FiltroPeriodo>();

        // Fire-and-forget: the query handler is held pending, so awaiting this dispatch fully would
        // deadlock. Bunit still processes the synchronous portion of the render (setting _carregando
        // and calling StateHasChanged before the awaited Mediator.Send) before returning control here.
        _ = cut.InvokeAsync(() => filtro.Instance.OnFiltroAplicado.InvokeAsync((inicio, fim)));

        // Spinner should reappear while the re-filtered query is pending.
        cut.WaitForState(() => cut.Markup.Contains("Carregando seu painel"));

        var reFilteredDto = BuildDto(1m, 1m);
        pending.SetResult(reFilteredDto);
        cut.WaitForState(() => !cut.Markup.Contains("Carregando seu painel"));

        mediatorMock.Verify(
            m => m.Send(It.Is<GetDashboardQuery>(q => q.DataInicio == inicio && q.DataFim == fim), It.IsAny<CancellationToken>()),
            Times.Once);

        await DisposeAsync();
    }
}
