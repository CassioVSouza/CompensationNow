using CompenseAgora.Common;

namespace CompenseAgora.Tests.Common;

public class EscalaEixoYTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(0.03, 1)]
    [InlineData(4.2, 1)]
    [InlineData(9.9, 2)]
    [InlineData(48, 10)]
    [InlineData(432, 100)]
    public void CalcularEspacamento_ReturnsStepScaledToData_NotAFixedDefault(double maiorValor, int espacamentoEsperado)
    {
        var espacamento = EscalaEixoY.CalcularEspacamento(maiorValor);

        Assert.Equal(espacamentoEsperado, espacamento);
    }

    [Fact]
    public void CalcularEspacamento_NeverReturnsTheOldFixedTwentyDefault_ForSmallData()
    {
        var espacamento = EscalaEixoY.CalcularEspacamento(0.05);

        Assert.NotEqual(20, espacamento);
        Assert.True(espacamento >= 1);
    }
}
