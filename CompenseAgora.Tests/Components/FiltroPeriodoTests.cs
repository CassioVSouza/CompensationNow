using Bunit;
using CompenseAgora.Components.Shared;
using CompenseAgora.Features.Common;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace CompenseAgora.Tests.Components;

/// <summary>
/// bUnit component tests for Components/Shared/FiltroPeriodo.razor. MudSelect/MudNumericField/
/// MudDatePicker values are driven through their component instances' ValueChanged/DateChanged
/// callbacks directly (the same callbacks @bind-Value/@bind-Date wire up to the parent), rather than
/// simulating raw DOM events against MudBlazor's popover-based markup, which is brittle under bUnit.
///
/// The "Aplicar filtro" button's enabled/disabled state is asserted from rendered markup (the
/// &lt;button disabled&gt; attribute) rather than the MudButton component's <c>Disabled</c> parameter
/// property directly: MudBlazor 9.x stores parameter values behind an internal "parameter state"
/// wrapper (see analyzer rule MUD0012), and reading simple bool/nullable-value parameters straight off
/// the component instance after a bUnit-driven render can return a stale snapshot even though the
/// actual rendered markup - and the app's real behavior - is already correct.
///
/// Every test ends with an explicit <c>await DisposeAsync()</c> - see the note on DashboardTests for
/// why: MudBlazor 9.10.0's internal PointerEventsNoneService (resolved as soon as a MudSelect renders)
/// implements only IAsyncDisposable, which crashes bUnit's synchronous teardown unless we dispose the
/// context asynchronously ourselves first.
/// </summary>
public class FiltroPeriodoTests : BunitContext
{
    private IRenderedComponent<FiltroPeriodo> RenderFiltro(Action<(DateOnly Inicio, DateOnly Fim)>? onAplicado = null)
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        return Render<FiltroPeriodo>(parameters => parameters
            .Add(p => p.OnFiltroAplicado, onAplicado ?? (_ => { })));
    }

    private static bool AplicarButtonIsDisabled(IRenderedComponent<FiltroPeriodo> cut) =>
        cut.FindAll("button").Single(b => b.TextContent.Contains("Aplicar filtro")).HasAttribute("disabled");

    [Fact]
    public async Task DefaultMode_IsUltimos12Meses_AndAplicarButtonIsEnabled()
    {
        var cut = RenderFiltro();

        var select = cut.FindComponent<MudSelect<TipoPeriodoFiltro>>();
        Assert.Equal(TipoPeriodoFiltro.Ultimos12Meses, select.Instance.Value);

        Assert.False(AplicarButtonIsDisabled(cut));

        // No extra mode-specific inputs shown for Últimos 12 meses.
        Assert.Empty(cut.FindComponents<MudDatePicker>());
        Assert.Empty(cut.FindComponents<MudNumericField<int?>>());
        Assert.Single(cut.FindComponents<MudSelect<TipoPeriodoFiltro>>());

        await DisposeAsync();
    }

    [Fact]
    public async Task Mensal_AplicarButton_IsDisabledUntilMesAndAnoAreFilled()
    {
        var cut = RenderFiltro();
        var select = cut.FindComponent<MudSelect<TipoPeriodoFiltro>>();

        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(TipoPeriodoFiltro.Mensal));

        // Reveals month + year inputs.
        var mesSelect = cut.FindComponents<MudSelect<int?>>().Single();
        var anoField = cut.FindComponents<MudNumericField<int?>>().Single();

        // Clear both - default field initializers pre-fill mes/ano with today's values, so the button
        // starts enabled; explicitly null them out to exercise the disabled path.
        await cut.InvokeAsync(() => mesSelect.Instance.ValueChanged.InvokeAsync(null));
        await cut.InvokeAsync(() => anoField.Instance.ValueChanged.InvokeAsync(null));

        Assert.True(AplicarButtonIsDisabled(cut));

        await cut.InvokeAsync(() => mesSelect.Instance.ValueChanged.InvokeAsync(5));
        await cut.InvokeAsync(() => anoField.Instance.ValueChanged.InvokeAsync(2026));

        Assert.False(AplicarButtonIsDisabled(cut));

        await DisposeAsync();
    }

    [Fact]
    public async Task Personalizado_AplicarButton_IsDisabledUntilBothDatesAreFilled_AndWhenInicioAfterFim()
    {
        var cut = RenderFiltro();
        var select = cut.FindComponent<MudSelect<TipoPeriodoFiltro>>();

        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(TipoPeriodoFiltro.Personalizado));

        var datePickers = cut.FindComponents<MudDatePicker>();
        Assert.Equal(2, datePickers.Count);
        var dataInicioPicker = datePickers[0];
        var dataFimPicker = datePickers[1];

        Assert.True(AplicarButtonIsDisabled(cut));

        await cut.InvokeAsync(() => dataInicioPicker.Instance.DateChanged.InvokeAsync(new DateTime(2026, 3, 10)));
        Assert.True(AplicarButtonIsDisabled(cut));

        await cut.InvokeAsync(() => dataFimPicker.Instance.DateChanged.InvokeAsync(new DateTime(2026, 3, 1)));
        Assert.True(AplicarButtonIsDisabled(cut), "Início posterior ao fim deve manter o botão desabilitado.");

        await cut.InvokeAsync(() => dataFimPicker.Instance.DateChanged.InvokeAsync(new DateTime(2026, 3, 20)));
        Assert.False(AplicarButtonIsDisabled(cut));

        await DisposeAsync();
    }

    [Fact]
    public async Task Anual_AplicarButton_IsDisabledUntilAnoIsFilled()
    {
        var cut = RenderFiltro();
        var select = cut.FindComponent<MudSelect<TipoPeriodoFiltro>>();

        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(TipoPeriodoFiltro.Anual));

        var anoField = cut.FindComponents<MudNumericField<int?>>().Single();
        await cut.InvokeAsync(() => anoField.Instance.ValueChanged.InvokeAsync(null));

        Assert.True(AplicarButtonIsDisabled(cut));

        await cut.InvokeAsync(() => anoField.Instance.ValueChanged.InvokeAsync(2026));
        Assert.False(AplicarButtonIsDisabled(cut));

        await DisposeAsync();
    }

    [Fact]
    public async Task SelectingEachMode_RevealsTheCorrectConditionalInputs()
    {
        var cut = RenderFiltro();
        var select = cut.FindComponent<MudSelect<TipoPeriodoFiltro>>();

        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(TipoPeriodoFiltro.Mensal));
        Assert.Single(cut.FindComponents<MudSelect<int?>>()); // month select
        Assert.Single(cut.FindComponents<MudNumericField<int?>>()); // year field
        Assert.Empty(cut.FindComponents<MudDatePicker>());

        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(TipoPeriodoFiltro.Personalizado));
        Assert.Empty(cut.FindComponents<MudSelect<int?>>());
        Assert.Empty(cut.FindComponents<MudNumericField<int?>>());
        Assert.Equal(2, cut.FindComponents<MudDatePicker>().Count);

        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(TipoPeriodoFiltro.Anual));
        Assert.Empty(cut.FindComponents<MudSelect<int?>>());
        Assert.Single(cut.FindComponents<MudNumericField<int?>>()); // year field only
        Assert.Empty(cut.FindComponents<MudDatePicker>());

        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(TipoPeriodoFiltro.Ultimos12Meses));
        Assert.Empty(cut.FindComponents<MudSelect<int?>>());
        Assert.Empty(cut.FindComponents<MudNumericField<int?>>());
        Assert.Empty(cut.FindComponents<MudDatePicker>());

        await DisposeAsync();
    }

    [Fact]
    public async Task ClickingAplicar_InMensalMode_InvokesOnFiltroAplicado_WithFirstAndLastDayOfMonth()
    {
        (DateOnly Inicio, DateOnly Fim)? captured = null;
        var cut = RenderFiltro(onAplicado: intervalo => captured = intervalo);

        var select = cut.FindComponent<MudSelect<TipoPeriodoFiltro>>();
        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(TipoPeriodoFiltro.Mensal));

        var mesSelect = cut.FindComponents<MudSelect<int?>>().Single();
        var anoField = cut.FindComponents<MudNumericField<int?>>().Single();
        await cut.InvokeAsync(() => mesSelect.Instance.ValueChanged.InvokeAsync(4));
        await cut.InvokeAsync(() => anoField.Instance.ValueChanged.InvokeAsync(2026));

        var button = cut.FindAll("button").Single(b => b.TextContent.Contains("Aplicar filtro"));
        await cut.InvokeAsync(() => button.Click());

        Assert.NotNull(captured);
        Assert.Equal(new DateOnly(2026, 4, 1), captured!.Value.Inicio);
        Assert.Equal(new DateOnly(2026, 4, 30), captured.Value.Fim);
        Assert.Contains("Exibindo:", cut.Markup);

        await DisposeAsync();
    }

    [Fact]
    public async Task ClickingAplicar_InAnualMode_InvokesOnFiltroAplicado_WithFullYearRange()
    {
        (DateOnly Inicio, DateOnly Fim)? captured = null;
        var cut = RenderFiltro(onAplicado: intervalo => captured = intervalo);

        var select = cut.FindComponent<MudSelect<TipoPeriodoFiltro>>();
        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(TipoPeriodoFiltro.Anual));

        var anoField = cut.FindComponents<MudNumericField<int?>>().Single();
        await cut.InvokeAsync(() => anoField.Instance.ValueChanged.InvokeAsync(2025));

        var button = cut.FindAll("button").Single(b => b.TextContent.Contains("Aplicar filtro"));
        await cut.InvokeAsync(() => button.Click());

        Assert.NotNull(captured);
        Assert.Equal(new DateOnly(2025, 1, 1), captured!.Value.Inicio);
        Assert.Equal(new DateOnly(2025, 12, 31), captured.Value.Fim);

        await DisposeAsync();
    }
}
