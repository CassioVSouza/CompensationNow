using Bunit;
using CompenseAgora.Components.Pages.Faq;
using MudBlazor;
using MudBlazor.Services;

namespace CompenseAgora.Tests.Components;

/// <summary>
/// bUnit tests for the public FAQ page. Search input is driven through MudTextField's ValueChanged
/// callback, as in FiltroPeriodoTests. Each test ends with <c>await DisposeAsync()</c> for the MudBlazor
/// IAsyncDisposable teardown issue described on DashboardTests.
/// </summary>
public class PerguntasFrequentesTests : BunitContext
{
    private IRenderedComponent<PerguntasFrequentes> RenderFaq()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        return Render<PerguntasFrequentes>();
    }

    private static Task BuscarAsync(IRenderedComponent<PerguntasFrequentes> cut, string termo) =>
        cut.InvokeAsync(() => cut.FindComponent<MudTextField<string>>().Instance.ValueChanged.InvokeAsync(termo));

    [Fact]
    public async Task WithoutSearch_ShowsEverySectionAndQuestion()
    {
        var cut = RenderFaq();

        Assert.Equal(ConteudoFaq.Secoes.Count, cut.FindAll("section.faq-secao").Count);
        Assert.Equal(ConteudoFaq.Secoes.Sum(s => s.Perguntas.Count), cut.FindComponents<MudExpansionPanel>().Count);

        await DisposeAsync();
    }

    [Fact]
    public async Task Search_IgnoresAccentsAndCase_AndMatchesAnswerText()
    {
        var cut = RenderFaq();

        // "biogenico" only appears (accented) inside an answer body.
        await BuscarAsync(cut, "BIOGENICO");

        var perguntas = cut.FindComponents<MudExpansionPanel>().Select(p => p.Instance.Text).ToList();
        Assert.Contains("Por que o etanol e o biodiesel “emitem menos” no cálculo?", perguntas);
        Assert.All(cut.FindComponents<MudExpansionPanel>(), p => Assert.True(p.Instance.Expanded));
        Assert.Contains("encontrada", cut.Markup);

        await DisposeAsync();
    }

    [Fact]
    public async Task Search_WithNoMatches_ShowsEmptyState_AndClearRestoresList()
    {
        var cut = RenderFaq();

        await BuscarAsync(cut, "xyzinexistente");

        Assert.Empty(cut.FindComponents<MudExpansionPanel>());
        Assert.Contains("Nenhuma pergunta encontrada", cut.Markup);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Limpar busca")).Click();

        Assert.Equal(ConteudoFaq.Secoes.Sum(s => s.Perguntas.Count), cut.FindComponents<MudExpansionPanel>().Count);

        await DisposeAsync();
    }
}
