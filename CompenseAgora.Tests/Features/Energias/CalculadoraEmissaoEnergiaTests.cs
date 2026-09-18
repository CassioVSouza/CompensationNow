using CompenseAgora.Entities;
using CompenseAgora.Features.Energias.Calculo;
using CompenseAgora.Tests.Infrastructure;

namespace CompenseAgora.Tests.Features.Energias;

public class CalculadoraEmissaoEnergiaTests
{
    [Fact]
    public async Task CalcularEmissaoCO2Async_FatorExatoParaMesEAno_MultiplicaQuantidadePeloFeSin()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        seedContext.FatoresEnergia.Add(new FatorEnergia { Mes = 6, Ano = 2026, FeSin = 0.08m });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoEnergia(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(new DateOnly(2026, 6, 15), quantidade: 500m);

        // 500 kWh * 0.08 kg CO2e/kWh = 40
        Assert.Equal(40m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_SemFatorParaOMes_UsaFatorAnteriorMaisRecente()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        seedContext.FatoresEnergia.AddRange(
            new FatorEnergia { Mes = 3, Ano = 2026, FeSin = 0.05m },
            new FatorEnergia { Mes = 5, Ano = 2026, FeSin = 0.09m });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoEnergia(dbContext);

        // Referência em julho/2026, sem fator cadastrado -> usa o mais recente anterior (maio/2026).
        var emissao = await calculadora.CalcularEmissaoCO2Async(new DateOnly(2026, 7, 1), quantidade: 200m);

        Assert.Equal(18m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_ReferenciaAnteriorATodosOsFatores_UsaOMaisAntigoDisponivel()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        seedContext.FatoresEnergia.AddRange(
            new FatorEnergia { Mes = 1, Ano = 2025, FeSin = 0.07m },
            new FatorEnergia { Mes = 6, Ano = 2026, FeSin = 0.09m });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoEnergia(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(new DateOnly(2024, 1, 1), quantidade: 100m);

        Assert.Equal(7m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_SemFatoresCadastrados_RetornaZero()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoEnergia(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(new DateOnly(2026, 1, 1), quantidade: 100m);

        Assert.Equal(0m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_QuantidadeZero_RetornaZero()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        seedContext.FatoresEnergia.Add(new FatorEnergia { Mes = 1, Ano = 2026, FeSin = 0.08m });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoEnergia(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(new DateOnly(2026, 1, 1), quantidade: 0m);

        Assert.Equal(0m, emissao);
    }
}
