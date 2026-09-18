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
/// </summary>
public class DashboardTests : BunitContext
{
    private static DashboardDto BuildDto(decimal totalViagem, decimal totalEnergia, Action<List<MonthlyEmissionDto>>? configureMonths = null)
    {
        var months = Enumerable.Range(0, 12)
            .Select(i => new MonthlyEmissionDto(2026, i + 1, 0m, 0m))
            .ToList();
        configureMonths?.Invoke(months);

        return new DashboardDto(months, totalViagem + totalEnergia, totalViagem, totalEnergia);
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
    public void Renders_LoadingSpinner_BeforeQueryResolves()
    {
        var pending = new TaskCompletionSource<DashboardDto>();
        SetUpMediator(dto: null, pending: pending);

        var cut = Render<Dashboard>();

        Assert.Contains("Carregando seu painel", cut.Markup);
        Assert.NotEmpty(cut.FindComponents<MudProgressCircular>());

        // Never resolved: no stat tiles or empty-state message should be present yet.
        Assert.DoesNotContain("Nenhuma emissão registrada ainda", cut.Markup);
        Assert.DoesNotContain("Emissão total", cut.Markup);
    }

    [Fact]
    public void Renders_EmptyState_WhenTotalEmissaoIsZero()
    {
        var dto = BuildDto(0m, 0m);
        SetUpMediator(dto);

        var cut = Render<Dashboard>();

        Assert.Contains("Nenhuma emissão registrada ainda", cut.Markup);
        Assert.Contains("/viagens/nova", cut.Markup);
        Assert.Contains("/energias/nova", cut.Markup);

        // No stat tiles or charts in the empty state.
        Assert.Empty(cut.FindComponents<MudChart<double>>());
    }

    [Fact]
    public void Renders_StatTiles_WithCorrectlyFormattedValues_WhenPopulated()
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

        var expectedTotal = (123.456m + 78.9m).ToString("N2", CultureInfo.CurrentCulture) + " kg CO2e";
        var expectedViagem = 123.456m.ToString("N2", CultureInfo.CurrentCulture) + " kg CO2e";
        var expectedEnergia = 78.9m.ToString("N2", CultureInfo.CurrentCulture) + " kg CO2e";

        Assert.Contains(expectedTotal, cut.Markup);
        Assert.Contains(expectedViagem, cut.Markup);
        Assert.Contains(expectedEnergia, cut.Markup);
    }

    [Fact]
    public void Renders_BothCharts_WithMonthLabelsAndCorrectSeriesCounts_WhenPopulated()
    {
        var dto = BuildDto(totalViagem: 50m, totalEnergia: 30m, months =>
        {
            months[0] = months[0] with { ViagemEmissao = 50m };
            months[6] = months[6] with { EnergiaEmissao = 30m };
        });
        SetUpMediator(dto);

        var cut = Render<Dashboard>();

        var charts = cut.FindComponents<MudChart<double>>();
        Assert.Equal(2, charts.Count);

        var lineChart = charts.Single(c => c.Instance.ChartType == ChartType.Line);
        var barChart = charts.Single(c => c.Instance.ChartType == ChartType.StackedBar);

        // Total-trend line chart: a single "Total emissions" series.
        Assert.Single(lineChart.Instance.ChartSeries);
        Assert.Equal("Emissão total", lineChart.Instance.ChartSeries[0].Name);
        Assert.Equal(12, lineChart.Instance.ChartSeries[0].Data.Count);

        // Travel-vs-energy stacked bar chart: two series, Viagens and Energia.
        Assert.Equal(2, barChart.Instance.ChartSeries.Count);
        Assert.Equal(["Viagens", "Energia"], barChart.Instance.ChartSeries.Select(s => s.Name).ToArray());

        // Both charts share the same 12 "MMM/yy" month labels, oldest first.
        var expectedLabels = dto.MonthlyEmissions
            .Select(m => new DateTime(m.Year, m.Month, 1).ToString("MMM/yy", CultureInfo.InvariantCulture))
            .ToArray();
        Assert.Equal(expectedLabels, lineChart.Instance.ChartLabels);
        Assert.Equal(expectedLabels, barChart.Instance.ChartLabels);
    }
}
