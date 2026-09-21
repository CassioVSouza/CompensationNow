using System.Security.Claims;
using Bunit;
using CompenseAgora.Components.Pages.Relatorios;
using CompenseAgora.Features.Compensacoes;
using CompenseAgora.Features.Compensacoes.Queries.GetCompensacoes;
using CompenseAgora.Features.Dashboard;
using CompenseAgora.Features.Dashboard.Queries.GetDashboard;
using CompenseAgora.Features.Energias;
using CompenseAgora.Features.Energias.Queries.GetEnergias;
using CompenseAgora.Features.Viagens;
using CompenseAgora.Features.Viagens.Queries.GetViagens;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;

namespace CompenseAgora.Tests.Components;

/// <summary>
/// bUnit component tests for Components/Pages/Relatorios/Relatorio.razor. IMediator is mocked so these
/// tests don't touch the real query handlers or a database - see DashboardTests for the rationale
/// behind rendering Interactive Server components directly with bUnit, and for why every test ends
/// with an explicit <c>await DisposeAsync()</c> (MudBlazor 9.10.0's PointerEventsNoneService, resolved
/// by FiltroPeriodo's MudSelect, is only IAsyncDisposable and breaks bUnit's synchronous teardown
/// otherwise).
/// </summary>
public class RelatorioTests : BunitContext
{
    private static DashboardDto EmptyDashboard() =>
        new([], 0m, 0m, 0m, 0m);

    private Mock<IMediator> SetUpMediator(DashboardDto? dto, TaskCompletionSource<DashboardDto>? pending = null)
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDashboardQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => pending is not null ? pending.Task : Task.FromResult(dto!));
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetViagensQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetEnergiasQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCompensacoesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        Services.AddSingleton(mediatorMock.Object);

        var authContext = this.AddAuthorization();
        authContext.SetAuthorized("Ana Silva");
        authContext.SetClaims(
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.GivenName, "Ana"),
            new Claim(ClaimTypes.Surname, "Silva"));

        return mediatorMock;
    }

    [Fact]
    public async Task Renders_LoadingSpinner_BeforeQueriesResolve()
    {
        var pending = new TaskCompletionSource<DashboardDto>();
        SetUpMediator(dto: null, pending: pending);

        var cut = Render<Relatorio>();

        Assert.Contains("Carregando seu relatório", cut.Markup);
        Assert.NotEmpty(cut.FindComponents<MudProgressCircular>());
        Assert.DoesNotContain("Sobre este relatório", cut.Markup);

        await DisposeAsync();
    }

    [Fact]
    public async Task Renders_EmptyState_WhenTotalEmissaoIsZero()
    {
        SetUpMediator(EmptyDashboard());

        var cut = Render<Relatorio>();

        Assert.Contains("Nenhuma emissão registrada ainda", cut.Markup);
        Assert.Contains("Sobre este relatório", cut.Markup);

        await DisposeAsync();
    }

    [Fact]
    public async Task Renders_Normally_WhenNoEmissionsButHasCompensacao()
    {
        var dto = new DashboardDto(
            [new MonthlyEmissionDto(2026, 6, 0m, 0m, 15m)],
            TotalEmissao: 0m,
            TotalViagem: 0m,
            TotalEnergia: 0m,
            TotalCompensacao: 15m);
        SetUpMediator(dto);

        var cut = Render<Relatorio>();

        Assert.DoesNotContain("Nenhuma emissão registrada ainda", cut.Markup);
        Assert.Contains("Emissão total", cut.Markup);

        await DisposeAsync();
    }

    [Fact]
    public async Task ReFiltering_SendsAllFourQueriesWithAppliedDates_AndShowsSpinnerAgainWhileReloading()
    {
        var initialDto = EmptyDashboard();
        var mediatorMock = SetUpMediator(dto: initialDto);

        var pending = new TaskCompletionSource<DashboardDto>();
        var callCount = 0;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDashboardQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                return callCount == 1 ? Task.FromResult(initialDto) : pending.Task;
            });

        var cut = Render<Relatorio>();
        Assert.Contains("Sobre este relatório", cut.Markup);

        var inicio = new DateOnly(2026, 1, 1);
        var fim = new DateOnly(2026, 3, 31);

        var filtro = cut.FindComponent<CompenseAgora.Components.Shared.FiltroPeriodo>();
        _ = cut.InvokeAsync(() => filtro.Instance.OnFiltroAplicado.InvokeAsync((inicio, fim)));

        cut.WaitForState(() => cut.Markup.Contains("Carregando seu relatório"));

        pending.SetResult(EmptyDashboard());
        cut.WaitForState(() => !cut.Markup.Contains("Carregando seu relatório"));

        mediatorMock.Verify(
            m => m.Send(It.Is<GetDashboardQuery>(q => q.DataInicio == inicio && q.DataFim == fim), It.IsAny<CancellationToken>()),
            Times.Once);
        mediatorMock.Verify(
            m => m.Send(It.Is<GetViagensQuery>(q => q.DataInicio == inicio && q.DataFim == fim), It.IsAny<CancellationToken>()),
            Times.Once);
        mediatorMock.Verify(
            m => m.Send(It.Is<GetEnergiasQuery>(q => q.DataInicio == inicio && q.DataFim == fim), It.IsAny<CancellationToken>()),
            Times.Once);
        mediatorMock.Verify(
            m => m.Send(It.Is<GetCompensacoesQuery>(q => q.DataInicio == inicio && q.DataFim == fim), It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Contains("01/01/2026 a 31/03/2026", cut.Markup);

        await DisposeAsync();
    }
}
