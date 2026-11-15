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
    public async Task CalcularEmissaoCO2Async_PorFrota_ConsumoDireto_UsaFatorDeEmissaoDaFrotaParaCh4ENo2o()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);

        var gnv = TestDataFactory.CreateCombustivel(seedContext, "Gás Natural Veicular (GNV)");
        var frota = TestDataFactory.CreateFrota(seedContext, gnv);

        seedContext.FatoresDoCombustivel.Add(new FatorCombustivel
        {
            CodigoCombustivel = gnv.Codigo,
            Ano = 2026,
            PoderCalorificoInferior = 0,
            Densidade = 0,
            CO2 = 2000m,
            CH4 = 999m, // não deve ser usado no modo "por frota" - só serve para provar que não vaza aqui.
            N2O = 999m,
        });
        seedContext.FatoresEmissaoFrota.Add(new FatorEmissaoFrota
        {
            CodigoFrota = frota.Codigo,
            Ano = 2020,
            CH4 = 0.5m,
            N2O = 0.1m,
        });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, codigoCombustivel: null, new DateOnly(2026, 6, 1), consumo: 100m, anoFrota: 2020, distanciaKm: 0m);

        // CO2 = 2000*100/1000 = 200; CH4 = 0.5*100/1000 = 0.05 (fator da frota, não do combustível); N2O = 0.1*100/1000 = 0.01
        // Total = 200*1 + 0.05*25 + 0.01*298 = 204.23
        Assert.Equal(204.23m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_PorFrota_SemFatorDeEmissaoDaFrotaCadastrado_Ch4ENo2oFicamZerados()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);

        var gnv = TestDataFactory.CreateCombustivel(seedContext, "Gás Natural Veicular (GNV)");
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
            frota.Codigo, codigoCombustivel: null, new DateOnly(2026, 6, 1), consumo: 100m, anoFrota: 2020, distanciaKm: 0m);

        // Sem FATOR_EMISSAO_FROTA cadastrado: CH4 = N2O = 0. Só sobra o CO2 do fator de combustível.
        // Total = 2000*100/1000 * 1 = 200
        Assert.Equal(200m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_PorFrota_GnvComBiometanoConfigurado_ConsumoFossilEBiogenicoIntegrais()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);

        // GNV e Biometano são, cada um, um dos nomes reconhecidos com "passagem integral" (sem redução por
        // percentual) nos seus próprios ramos — a planilha oficial não zera nenhum dos dois nesse caso.
        var gnv = TestDataFactory.CreateCombustivel(seedContext, "Gás Natural Veicular (GNV)");
        var biometano = TestDataFactory.CreateCombustivel(seedContext, "Biometano");

        var frota = new Frota
        {
            Nome = "Frota GNV",
            CodigoCombustivelPrimario = gnv.Codigo,
            CodigoCombustivelFossil = gnv.Codigo,
            CodigoCombustivelBiogenico = biometano.Codigo,
        };
        seedContext.Frotas.Add(frota);
        await seedContext.SaveChangesAsync();

        seedContext.FatoresDoCombustivel.Add(
            new FatorCombustivel { CodigoCombustivel = gnv.Codigo, Ano = 2026, CO2 = 2000m, CH4 = 999m, N2O = 999m });
        seedContext.FatoresEmissaoFrota.Add(new FatorEmissaoFrota
        {
            CodigoFrota = frota.Codigo,
            Ano = 2020,
            CH4 = 0.2m,
            N2O = 0.05m,
        });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, codigoCombustivel: null, new DateOnly(2026, 6, 15), consumo: 100m, anoFrota: 2020, distanciaKm: 0m);

        // consumoFossil = 100 (GNV, passagem integral) -> CO2 = 2000*100/1000 = 200.
        // CH4/N2O usam sempre o consumo bruto (100), não a soma das parcelas: CH4 = 0.2*100/1000 = 0.02;
        // N2O = 0.05*100/1000 = 0.005.
        // Total = 200 + 0.02*25 + 0.005*298 = 201.99
        Assert.Equal(201.99m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_PorFrota_CombustivelNaoReconhecidoSemBiogenico_ZeraApenasConsumoFossil()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);

        // "Eletricidade" não é um dos três nomes fósseis reconhecidos (gasolina pura, diesel puro, GNV):
        // ao contrário do modo "por combustível", aqui o consumo fóssil NÃO passa integralmente — é zerado.
        // CH4/N2O, porém, sempre usam o fator da frota aplicado ao consumo bruto, independentemente disso.
        var eletricidade = TestDataFactory.CreateCombustivel(seedContext, "Eletricidade");
        var frota = TestDataFactory.CreateFrota(seedContext, eletricidade);

        seedContext.FatoresDoCombustivel.Add(new FatorCombustivel
        {
            CodigoCombustivel = eletricidade.Codigo,
            Ano = 2026,
            CO2 = 2000m,
            CH4 = 999m,
            N2O = 999m,
        });
        seedContext.FatoresEmissaoFrota.Add(new FatorEmissaoFrota
        {
            CodigoFrota = frota.Codigo,
            Ano = 2020,
            CH4 = 0.2m,
            N2O = 0.05m,
        });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, codigoCombustivel: null, new DateOnly(2026, 6, 15), consumo: 100m, anoFrota: 2020, distanciaKm: 0m);

        // consumoFossil = 0 (nome não reconhecido) -> CO2 = 0.
        // CH4 = 0.2*100/1000 = 0.02; N2O = 0.05*100/1000 = 0.005 (usam o consumo bruto, não a parcela fóssil).
        // Total = 0 + 0.02*25 + 0.005*298 = 1.99
        Assert.Equal(1.99m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_PorCombustivel_NaoPrecisaDeVeiculo_DivideEntreFossilEBiogenicoConformeOProprioCombustivel()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);

        // "Por combustível" não depende de nenhum veículo: o par fóssil/biogênico vem do próprio
        // combustível escolhido na viagem (Gasolina Comum), não de uma frota.
        var gasolinaPura = TestDataFactory.CreateCombustivel(seedContext, "Gasolina Automotiva (pura)");
        var etanol = TestDataFactory.CreateCombustivel(seedContext, "Etanol");
        seedContext.SaveChanges();

        var gasolinaComum = new Combustivel
        {
            Nome = "Gasolina Comum",
            UnidadeMedida = "L",
            CodigoCombustivelFossil = gasolinaPura.Codigo,
            CodigoCombustivelBiogenico = etanol.Codigo,
        };
        seedContext.Combustiveis.Add(gasolinaComum);

        seedContext.FatoresDoCombustivel.AddRange(
            new FatorCombustivel { CodigoCombustivel = gasolinaPura.Codigo, Ano = 2026, CO2 = 2200m, CH4 = 0.2m, N2O = 0.02m },
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
            codigoFrota: null, codigoCombustivel: gasolinaComum.Codigo, new DateOnly(2026, 6, 15), consumo: 100m, anoFrota: 0, distanciaKm: 0m);

        // 27% etanol -> consumoBiogenico=27, consumoFossil=73
        // CO2 = 2200*73/1000 = 160.6
        // CH4 = (0.2*73 + 0.1*27)/1000 = 0.0173; N2O = (0.02*73 + 0.01*27)/1000 = 0.00173
        // Total = 160.6 + 0.0173*25 + 0.00173*298 = 161.54804
        Assert.Equal(161.54804m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_PorCombustivel_SemParFossilBiogenicoNoCombustivel_TrataTudoComoFossil()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);

        // Combustível "simples", sem CombustivelFossil/CombustivelBiogenico configurados nele mesmo:
        // ObterFatorCombustivelAsync(codigoCombustivelFossil: null, ...) deve zerar o CO2 (não há fator
        // fóssil de referência a consultar) - reflete fielmente o comportamento do projeto original.
        var gnv = TestDataFactory.CreateCombustivel(seedContext, "GNV");
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
            codigoFrota: null, codigoCombustivel: gnv.Codigo, new DateOnly(2026, 6, 15), consumo: 100m, anoFrota: 0, distanciaKm: 0m);

        Assert.Equal(0m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_PorDistancia_UsaConsumoMedioDaFrotaParaDerivarConsumo()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);

        var diesel = TestDataFactory.CreateCombustivel(seedContext, "Óleo Diesel (puro)");
        var frota = TestDataFactory.CreateFrota(seedContext, diesel);

        seedContext.FatoresDoCombustivel.Add(new FatorCombustivel
        {
            CodigoCombustivel = diesel.Codigo,
            Ano = 2026,
            CO2 = 2000m,
            CH4 = 999m, // não deve ser usado no modo "por frota"/distância.
            N2O = 999m,
        });
        seedContext.ConsumosMedioFrota.Add(new ConsumoMedioFrota
        {
            CodigoFrota = frota.Codigo,
            Ano = 2020,
            Consumo = 10m,
            UnidadeMedida = "km/L",
        });
        seedContext.FatoresEmissaoFrota.Add(new FatorEmissaoFrota
        {
            CodigoFrota = frota.Codigo,
            Ano = 2020,
            CH4 = 0.5m,
            N2O = 0.1m,
        });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, codigoCombustivel: null, new DateOnly(2026, 6, 1), consumo: 0m, anoFrota: 2020, distanciaKm: 150m);

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
            frota.Codigo, codigoCombustivel: null, new DateOnly(2026, 6, 1), consumo: 0m, anoFrota: 2020, distanciaKm: 150m);

        Assert.Equal(0m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_FrotaInexistente_RetornaZero()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            999, codigoCombustivel: null, new DateOnly(2026, 6, 1), consumo: 100m, anoFrota: 2020, distanciaKm: 0m);

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
            frota.Codigo, codigoCombustivel: null, new DateOnly(2026, 6, 1), consumo: 50m, anoFrota: 2020, distanciaKm: 0m);

        Assert.Equal(0m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_SemGasesEfeitoEstufaCadastrados_UsaGwpPadraoUm()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        // Nenhum GasEfeitoEstufa semeado propositalmente.
        var gnv = TestDataFactory.CreateCombustivel(seedContext, "Gás Natural Veicular (GNV)");
        var frota = TestDataFactory.CreateFrota(seedContext, gnv);

        seedContext.FatoresDoCombustivel.Add(new FatorCombustivel
        {
            CodigoCombustivel = gnv.Codigo,
            Ano = 2026,
            CO2 = 2000m,
            CH4 = 0.5m,
            N2O = 0.1m,
        });
        seedContext.FatoresEmissaoFrota.Add(new FatorEmissaoFrota
        {
            CodigoFrota = frota.Codigo,
            Ano = 2020,
            CH4 = 0.5m,
            N2O = 0.1m,
        });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, codigoCombustivel: null, new DateOnly(2026, 6, 1), consumo: 100m, anoFrota: 2020, distanciaKm: 0m);

        // Sem GWP cadastrado, cada gás usa fator 1: 200 + 0.05 + 0.01 = 200.06
        Assert.Equal(200.06m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_AnoSemFatorExato_UsaUltimoFatorAnteriorDisponivel()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);
        var gnv = TestDataFactory.CreateCombustivel(seedContext, "Gás Natural Veicular (GNV)");
        var frota = TestDataFactory.CreateFrota(seedContext, gnv);

        seedContext.FatoresDoCombustivel.Add(new FatorCombustivel
        {
            CodigoCombustivel = gnv.Codigo,
            Ano = 2020,
            CO2 = 2000m,
            CH4 = 0.5m,
            N2O = 0.1m,
        });
        seedContext.FatoresEmissaoFrota.Add(new FatorEmissaoFrota
        {
            CodigoFrota = frota.Codigo,
            Ano = 2020,
            CH4 = 0.5m,
            N2O = 0.1m,
        });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        // Viagem referenciada em 2026, mas só existe fator de 2020 -> deve reaproveitar o mais recente disponível.
        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frota.Codigo, codigoCombustivel: null, new DateOnly(2026, 6, 1), consumo: 100m, anoFrota: 2020, distanciaKm: 0m);

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
            frota.Codigo, codigoCombustivel: null, new DateOnly(2026, 6, 1), consumo: 0m, anoFrota: 2020, distanciaKm: 0m);

        Assert.Equal(0m, emissao);
    }

    [Fact]
    public async Task CalcularEmissaoCO2Async_PorFrota_FrotaGlpSemFatorProprio_UsaFatorDaFrotaGasolinaPadrao()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        SeedGases(seedContext);

        var glpCombustivel = TestDataFactory.CreateCombustivel(seedContext, "Gás Liquefeito de Petróleo (GLP)");
        var frotaGlp = new Frota { Nome = "Gás Liquefeito de Petróleo (GLP)", CodigoCombustivelPrimario = glpCombustivel.Codigo };
        var frotaGasolina = new Frota { Nome = "Automóvel a gasolina", CodigoCombustivelPrimario = glpCombustivel.Codigo };
        seedContext.Frotas.AddRange(frotaGlp, frotaGasolina);
        await seedContext.SaveChangesAsync();

        seedContext.FatoresDoCombustivel.Add(new FatorCombustivel
        {
            CodigoCombustivel = glpCombustivel.Codigo,
            Ano = 2026,
            CO2 = 1500m,
            CH4 = 999m,
            N2O = 999m,
        });
        // Sem fator próprio para a frota de GLP - só existe um fator cadastrado para a frota "Automóvel a gasolina".
        seedContext.FatoresEmissaoFrota.Add(new FatorEmissaoFrota
        {
            CodigoFrota = frotaGasolina.Codigo,
            Ano = 2020,
            CH4 = 0.3m,
            N2O = 0.03m,
        });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var calculadora = new CalculadoraEmissaoViagem(dbContext);

        var emissao = await calculadora.CalcularEmissaoCO2Async(
            frotaGlp.Codigo, codigoCombustivel: null, new DateOnly(2026, 6, 1), consumo: 100m, anoFrota: 2020, distanciaKm: 0m);

        // "Gás Liquefeito de Petróleo (GLP)" não é um dos três nomes fósseis reconhecidos no modo "por
        // frota" (gasolina pura, diesel puro, GNV) -> consumoFossil = 0 -> CO2 = 0. O fallback de GLP só
        // afeta o fator de CH4/N2O (por veículo/ano), não a parcela de consumo fóssil.
        // CH4 = 0.3*100/1000 = 0.03 (fator da frota "Automóvel a gasolina"); N2O = 0.03*100/1000 = 0.003
        // Total = 0 + 0.03*25 + 0.003*298 = 1.644
        Assert.Equal(1.644m, emissao);
    }
}
