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

    private static Compensacao NewCompensacao(int codigoPessoa, DateOnly dataReferencia, decimal quantidade) =>
        new()
        {
            CodigoPessoa = codigoPessoa,
            DataReferencia = dataReferencia,
            CriadoEm = dataReferencia,
            TipoCompensacao = "Outro",
            QuantidadeCompensada = quantidade,
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
            Assert.Equal(0m, m.Compensacao);
            Assert.Equal(0m, m.Total);
        });
        Assert.Equal(0m, dto.TotalEmissao);
        Assert.Equal(0m, dto.TotalViagem);
        Assert.Equal(0m, dto.TotalEnergia);
        Assert.Equal(0m, dto.TotalCompensacao);

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

        // Current month also has a compensation record.
        seedContext.Compensacoes.Add(NewCompensacao(pessoa.Codigo, currentMonthStart, 4m));

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
        Assert.Equal(4m, lastMonth.Compensacao);
        Assert.Equal(13m, lastMonth.Total);

        Assert.Equal(15m, dto.TotalViagem);
        Assert.Equal(10m, dto.TotalEnergia);
        Assert.Equal(25m, dto.TotalEmissao);
        Assert.Equal(4m, dto.TotalCompensacao);

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
        var dto = new MonthlyEmissionDto(2026, 3, 12.5m, 7.25m, 5m);

        Assert.Equal(19.75m, dto.Total);
    }

    [Fact]
    public async Task Handle_WithExplicitDateRange_ZeroFillsOnlyMonthsIntersectingRange()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var frota = TestDataFactory.CreateFrota(seedContext);

        // Explicit 3-month range: Jan, Feb, Mar 2026.
        var inicio = new DateOnly(2026, 1, 1);
        var fim = new DateOnly(2026, 3, 31);

        seedContext.Viagens.Add(NewViagem(pessoa.Codigo, frota.Codigo, new DateOnly(2026, 2, 15), 8m));

        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetDashboardQueryHandler(dbContext);

        var dto = await handler.Handle(new GetDashboardQuery(pessoa.Codigo, inicio, fim), CancellationToken.None);

        Assert.Equal(3, dto.MonthlyEmissions.Count);
        Assert.Equal((2026, 1), (dto.MonthlyEmissions[0].Year, dto.MonthlyEmissions[0].Month));
        Assert.Equal((2026, 2), (dto.MonthlyEmissions[1].Year, dto.MonthlyEmissions[1].Month));
        Assert.Equal((2026, 3), (dto.MonthlyEmissions[2].Year, dto.MonthlyEmissions[2].Month));

        Assert.Equal(0m, dto.MonthlyEmissions[0].ViagemEmissao);
        Assert.Equal(8m, dto.MonthlyEmissions[1].ViagemEmissao);
        Assert.Equal(0m, dto.MonthlyEmissions[2].ViagemEmissao);
        Assert.Equal(8m, dto.TotalViagem);
    }

    [Fact]
    public async Task Handle_WithExplicitDateRange_ExcludesRecordsOutsideRange_EvenWithinAPartiallyIncludedMonth()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var frota = TestDataFactory.CreateFrota(seedContext);

        // Range spans mid-January to mid-February: the calendar-month bucket for January still
        // gets zero-filled, but a record from *before* DataInicio inside that same month must
        // still be excluded from the sums.
        var inicio = new DateOnly(2026, 1, 15);
        var fim = new DateOnly(2026, 2, 15);

        // Before the range, but within the January bucket month - must be excluded.
        seedContext.Viagens.Add(NewViagem(pessoa.Codigo, frota.Codigo, new DateOnly(2026, 1, 5), 100m));

        // Inside the range - must be included.
        seedContext.Viagens.Add(NewViagem(pessoa.Codigo, frota.Codigo, new DateOnly(2026, 1, 20), 3m));

        // After the range, but within the February bucket month - must be excluded.
        seedContext.Energias.Add(NewEnergia(pessoa.Codigo, new DateOnly(2026, 2, 20), 200m));

        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetDashboardQueryHandler(dbContext);

        var dto = await handler.Handle(new GetDashboardQuery(pessoa.Codigo, inicio, fim), CancellationToken.None);

        Assert.Equal(2, dto.MonthlyEmissions.Count);
        Assert.Equal(3m, dto.MonthlyEmissions[0].ViagemEmissao);
        Assert.Equal(0m, dto.MonthlyEmissions[1].EnergiaEmissao);
        Assert.Equal(3m, dto.TotalViagem);
        Assert.Equal(0m, dto.TotalEnergia);
    }

    [Fact]
    public async Task Handle_WithExplicitDateRange_IncludesRecordsExactlyOnBoundaryDates()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var frota = TestDataFactory.CreateFrota(seedContext);

        var inicio = new DateOnly(2026, 1, 10);
        var fim = new DateOnly(2026, 1, 20);

        seedContext.Viagens.Add(NewViagem(pessoa.Codigo, frota.Codigo, inicio, 2m));
        seedContext.Viagens.Add(NewViagem(pessoa.Codigo, frota.Codigo, fim, 5m));

        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetDashboardQueryHandler(dbContext);

        var dto = await handler.Handle(new GetDashboardQuery(pessoa.Codigo, inicio, fim), CancellationToken.None);

        Assert.Equal(7m, dto.TotalViagem);
    }
}
