using CompenseAgora.Entities;
using CompenseAgora.Features.Dashboard;
using CompenseAgora.Features.Dashboard.Queries.GetDashboard;
using CompenseAgora.Tests.Infrastructure;

namespace CompenseAgora.Tests.Features.Dashboard;

public class GetDashboardQueryTests
{
    private static DateOnly CurrentMonthStart()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new DateOnly(today.Year, today.Month, 1);
    }

    private static Viagem NewViagem(int codigoPessoa, int codigoFrota, DateOnly dataReferencia, decimal emissao) =>
        new()
        {
            CodigoPessoa = codigoPessoa,
            CodigoFrota = codigoFrota,
            CriadoEm = dataReferencia,
            DataReferencia = dataReferencia,
            Consumo = 1m,
            AnoFrota = 2020,
            DistanciaKM = 10m,
            EmissaoCO2 = emissao,
        };

    private static Energia NewEnergia(int codigoPessoa, DateOnly dataReferencia, decimal emissao) =>
        new()
        {
            CodigoPessoa = codigoPessoa,
            DataReferencia = dataReferencia,
            Quantidade = 1m,
            CriadoEm = dataReferencia,
            EmissaoCO2 = emissao,
        };

    [Fact]
    public async Task Handle_ReturnsTwelveZeroFilledMonths_WhenPersonHasNoRecords()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);

        await using var dbContext = factory.CreateContext();
        var handler = new GetDashboardQueryHandler(dbContext);

        var dto = await handler.Handle(new GetDashboardQuery(pessoa.Codigo), CancellationToken.None);

        Assert.Equal(12, dto.MonthlyEmissions.Count);
        Assert.All(dto.MonthlyEmissions, m =>
        {
            Assert.Equal(0m, m.ViagemEmissao);
            Assert.Equal(0m, m.EnergiaEmissao);
            Assert.Equal(0m, m.Total);
        });
        Assert.Equal(0m, dto.TotalEmissao);
        Assert.Equal(0m, dto.TotalViagem);
        Assert.Equal(0m, dto.TotalEnergia);

        var currentMonthStart = CurrentMonthStart();
        var windowStart = currentMonthStart.AddMonths(-11);
        Assert.Equal((windowStart.Year, windowStart.Month), (dto.MonthlyEmissions[0].Year, dto.MonthlyEmissions[0].Month));
        Assert.Equal((currentMonthStart.Year, currentMonthStart.Month), (dto.MonthlyEmissions[11].Year, dto.MonthlyEmissions[11].Month));
    }

    [Fact]
    public async Task Handle_ComputesCorrectPerMonthSumsAndTotals_ForMixedRecordsInsideWindow()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var frota = TestDataFactory.CreateFrota(seedContext);

        var currentMonthStart = CurrentMonthStart();
        var windowStart = currentMonthStart.AddMonths(-11);
        var midMonth = currentMonthStart.AddMonths(-5);

        // Current month: both a Viagem and an Energia record.
        seedContext.Viagens.Add(NewViagem(pessoa.Codigo, frota.Codigo, currentMonthStart, 10m));
        seedContext.Energias.Add(NewEnergia(pessoa.Codigo, currentMonthStart, 3m));

        // Oldest month in the window: Viagem only.
        seedContext.Viagens.Add(NewViagem(pessoa.Codigo, frota.Codigo, windowStart, 5m));

        // A month in the middle: Energia only.
        seedContext.Energias.Add(NewEnergia(pessoa.Codigo, midMonth, 7m));

        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetDashboardQueryHandler(dbContext);

        var dto = await handler.Handle(new GetDashboardQuery(pessoa.Codigo), CancellationToken.None);

        Assert.Equal(12, dto.MonthlyEmissions.Count);

        var firstMonth = dto.MonthlyEmissions[0];
        Assert.Equal((windowStart.Year, windowStart.Month), (firstMonth.Year, firstMonth.Month));
        Assert.Equal(5m, firstMonth.ViagemEmissao);
        Assert.Equal(0m, firstMonth.EnergiaEmissao);

        var middle = dto.MonthlyEmissions.Single(m => m.Year == midMonth.Year && m.Month == midMonth.Month);
        Assert.Equal(0m, middle.ViagemEmissao);
        Assert.Equal(7m, middle.EnergiaEmissao);

        var lastMonth = dto.MonthlyEmissions[11];
        Assert.Equal((currentMonthStart.Year, currentMonthStart.Month), (lastMonth.Year, lastMonth.Month));
        Assert.Equal(10m, lastMonth.ViagemEmissao);
        Assert.Equal(3m, lastMonth.EnergiaEmissao);
        Assert.Equal(13m, lastMonth.Total);

        Assert.Equal(15m, dto.TotalViagem);
        Assert.Equal(10m, dto.TotalEnergia);
        Assert.Equal(25m, dto.TotalEmissao);

        // Ascending chronological order across all 12 entries.
        for (var i = 1; i < dto.MonthlyEmissions.Count; i++)
        {
            var prev = dto.MonthlyEmissions[i - 1];
            var curr = dto.MonthlyEmissions[i];
            var prevDate = new DateOnly(prev.Year, prev.Month, 1);
            var currDate = new DateOnly(curr.Year, curr.Month, 1);
            Assert.Equal(prevDate.AddMonths(1), currDate);
        }
    }

    [Fact]
    public async Task Handle_ExcludesRecordsOutsideTheTwelveMonthWindow()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var frota = TestDataFactory.CreateFrota(seedContext);

        var currentMonthStart = CurrentMonthStart();
        var windowStart = currentMonthStart.AddMonths(-11);

        // 13 months old - one month before the window starts.
        seedContext.Viagens.Add(NewViagem(pessoa.Codigo, frota.Codigo, windowStart.AddMonths(-1), 100m));

        // One month in the future - after the window ends.
        seedContext.Energias.Add(NewEnergia(pessoa.Codigo, currentMonthStart.AddMonths(1), 50m));

        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetDashboardQueryHandler(dbContext);

        var dto = await handler.Handle(new GetDashboardQuery(pessoa.Codigo), CancellationToken.None);

        Assert.Equal(0m, dto.TotalEmissao);
        Assert.Equal(0m, dto.TotalViagem);
        Assert.Equal(0m, dto.TotalEnergia);
        Assert.All(dto.MonthlyEmissions, m => Assert.Equal(0m, m.Total));
    }

    [Fact]
    public async Task Handle_DoesNotLeakDataFromOtherPessoas()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa1 = TestDataFactory.CreatePessoa(seedContext, "p1@example.com");
        var pessoa2 = TestDataFactory.CreatePessoa(seedContext, "p2@example.com");
        var frota = TestDataFactory.CreateFrota(seedContext);

        var currentMonthStart = CurrentMonthStart();

        seedContext.Viagens.Add(NewViagem(pessoa2.Codigo, frota.Codigo, currentMonthStart, 20m));
        seedContext.Energias.Add(NewEnergia(pessoa2.Codigo, currentMonthStart, 15m));
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetDashboardQueryHandler(dbContext);

        var dto = await handler.Handle(new GetDashboardQuery(pessoa1.Codigo), CancellationToken.None);

        Assert.Equal(0m, dto.TotalEmissao);
        Assert.Equal(0m, dto.TotalViagem);
        Assert.Equal(0m, dto.TotalEnergia);
        Assert.All(dto.MonthlyEmissions, m => Assert.Equal(0m, m.Total));
    }

    [Fact]
    public void MonthlyEmissionDto_Total_EqualsSumOfViagemAndEnergiaEmissao()
    {
        var dto = new MonthlyEmissionDto(2026, 3, 12.5m, 7.25m);

        Assert.Equal(19.75m, dto.Total);
    }
}
