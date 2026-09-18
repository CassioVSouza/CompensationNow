using CompenseAgora.Entities;
using CompenseAgora.Features.Viagens.Calculo;
using CompenseAgora.Tests.Infrastructure;

namespace CompenseAgora.Tests.Features.Viagens;

public class CalculadoraEmissaoViagemTests
{
    private static void SeedGases(CompenseAgora.Data.CompenseAgoraDbContext dbContext)
    {
        dbContext.GasesEfeitoEstufa.AddRange(
            new GasEfeitoEstufa { Nome = "Dióxido de carbono (CO2)", Familia = "GEE", GWP = 1m },
            new GasEfeitoEstufa { Nome = "Metano (CH4)", Familia = "GEE", GWP = 25m },
            new GasEfeitoEstufa { Nome = "Óxido nitroso (N2O)", Familia = "GEE", GWP = 298m });
        dbContext.SaveChanges();
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_ConsumoDireto_CombustivelSemMistura_CalculaSomenteFrotaFossil()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);

        var gnv = TestDataFactory.CreateCombustivel(seedContext, "GNV");
        var frota = TestDataFactory.CreateFrota(seedContext, gnv);

        seedContext.FatoresDoCombustivel.Add(new FatorCombustivel
        {
            CodigoCombustivel = gnv.Codigo,
            Ano = 2026,
            PoderCalorificoInferior = 0,
            Densidade = 0,
            CO2 = 2000m,
            CH4 = 0.5m,
            N2O = 0.1m,
        });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, new DateOnly(2026, 6, 1), consumo: 100m, anoFrota: 2020, distanciaKm: 0m);

        // CO2 = 2000*100/1000 = 200; CH4 = 0.5*100/1000 = 0.05; N2O = 0.1*100/1000 = 0.01
        // Total = 200*1 + 0.05*25 + 0.01*298 = 204.23
        Assert.Equal(204.23m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_ConsumoDireto_CombustivelComMistura_DivideEntreFossilEBiogenico()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);

        var gasolina = TestDataFactory.CreateCombustivel(seedContext, "Gasolina");
        var etanol = TestDataFactory.CreateCombustivel(seedContext, "Etanol");

        var frota = new Frota
        {
            Nome = "Flex",
            CodigoCombustivelPrimario = gasolina.Codigo,
            CodigoCombustivelFossil = gasolina.Codigo,
            CodigoCombustivelBiogenico = etanol.Codigo,
        };
        seedContext.Frotas.Add(frota);

        seedContext.FatoresDoCombustivel.AddRange(
            new FatorCombustivel { CodigoCombustivel = gasolina.Codigo, Ano = 2026, CO2 = 2200m, CH4 = 0.2m, N2O = 0.02m },
            new FatorCombustivel { CodigoCombustivel = etanol.Codigo, Ano = 2026, CO2 = 0m, CH4 = 0.1m, N2O = 0.01m });

        seedContext.FatoresComposicaoCombustivel.Add(new FatorComposicaoCombustivel
        {
            Mes = 6,
            Ano = 2026,
            PercentualEtanol = 27m,
            PercentualBiodiesel = 0m,
        });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, new DateOnly(2026, 6, 15), consumo: 100m, anoFrota: 2026, distanciaKm: 0m);

        // 27% etanol -> consumoBiogenico=27, consumoFossil=73
        // CO2 = 2200*73/1000 = 160.6
        // CH4 = (0.2*73 + 0.1*27)/1000 = 0.0173; N2O = (0.02*73 + 0.01*27)/1000 = 0.00173
        // Total = 160.6 + 0.0173*25 + 0.00173*298 = 161.54804
        Assert.Equal(161.54804m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_PorDistancia_UsaConsumoMedioDaFrotaParaDerivarConsumo()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);

        var diesel = TestDataFactory.CreateCombustivel(seedContext, "Diesel");
        var frota = TestDataFactory.CreateFrota(seedContext, diesel);

        seedContext.FatoresDoCombustivel.Add(new FatorCombustivel
        {
            CodigoCombustivel = diesel.Codigo,
            Ano = 2026,
            CO2 = 2000m,
            CH4 = 0.5m,
            N2O = 0.1m,
        });
        seedContext.ConsumosMedioFrota.Add(new ConsumoMedioFrota
        {
            CodigoFrota = frota.Codigo,
            Ano = 2020,
            Consumo = 10m,
            UnidadeMedida = "km/L",
        });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, new DateOnly(2026, 6, 1), consumo: 0m, anoFrota: 2020, distanciaKm: 150m);

        // consumo derivado = 150/10 = 15L
        // CO2 = 2000*15/1000 = 30; CH4 = 0.5*15/1000 = 0.0075; N2O = 0.1*15/1000 = 0.0015
        // Total = 30 + 0.0075*25 + 0.0015*298 = 30.6345
        Assert.Equal(30.6345m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_PorDistancia_SemConsumoMedioCadastrado_RetornaZero()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);
        var frota = TestDataFactory.CreateFrota(seedContext);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, new DateOnly(2026, 6, 1), consumo: 0m, anoFrota: 2020, distanciaKm: 150m);

        Assert.Equal(0m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_FrotaInexistente_RetornaZero()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            999, new DateOnly(2026, 6, 1), consumo: 100m, anoFrota: 2020, distanciaKm: 0m);

        Assert.Equal(0m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_SemFatoresDeCombustivelCadastrados_RetornaZero()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);
        var frota = TestDataFactory.CreateFrota(seedContext);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, new DateOnly(2026, 6, 1), consumo: 50m, anoFrota: 2020, distanciaKm: 0m);

        Assert.Equal(0m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_SemGasesEfeitoEstufaCadastrados_UsaGwpPadraoUm()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        // Nenhum GasEfeitoEstufa semeado propositalmente.
        var gnv = TestDataFactory.CreateCombustivel(seedContext, "GNV");
        var frota = TestDataFactory.CreateFrota(seedContext, gnv);

        seedContext.FatoresDoCombustivel.Add(new FatorCombustivel
        {
            CodigoCombustivel = gnv.Codigo,
            Ano = 2026,
            CO2 = 2000m,
            CH4 = 0.5m,
            N2O = 0.1m,
        });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, new DateOnly(2026, 6, 1), consumo: 100m, anoFrota: 2020, distanciaKm: 0m);

        // Sem GWP cadastrado, cada gás usa fator 1: 200 + 0.05 + 0.01 = 200.06
        Assert.Equal(200.06m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_AnoSemFatorExato_UsaUltimoFatorAnteriorDisponivel()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);
        var gnv = TestDataFactory.CreateCombustivel(seedContext, "GNV");
        var frota = TestDataFactory.CreateFrota(seedContext, gnv);

        seedContext.FatoresDoCombustivel.Add(new FatorCombustivel
        {
            CodigoCombustivel = gnv.Codigo,
            Ano = 2020,
            CO2 = 2000m,
            CH4 = 0.5m,
            N2O = 0.1m,
        });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        // Viagem referenciada em 2026, mas só existe fator de 2020 -> deve reaproveitar o mais recente disponível.
        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, new DateOnly(2026, 6, 1), consumo: 100m, anoFrota: 2020, distanciaKm: 0m);

        Assert.Equal(204.23m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_SemConsumoESemDistancia_RetornaZero()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var frota = TestDataFactory.CreateFrota(seedContext);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, new DateOnly(2026, 6, 1), consumo: 0m, anoFrota: 2020, distanciaKm: 0m);

        Assert.Equal(0m, emissao);
    }
}
