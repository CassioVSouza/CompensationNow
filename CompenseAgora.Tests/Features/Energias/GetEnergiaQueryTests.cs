using CompenseAgora.Entities;
using CompenseAgora.Features.Energias.Queries.GetEnergiaById;
using CompenseAgora.Features.Energias.Queries.GetEnergias;
using CompenseAgora.Tests.Infrastructure;

namespace CompenseAgora.Tests.Features.Energias;

public class GetEnergiaByIdQueryTests
{
    [Fact]
    public async Task Handle_ReturnsDto_WhenEnergiaExists()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var energia = new Energia
        {
            CodigoPessoa = pessoa.Codigo,
            CriadoEm = new DateOnly(2026, 1, 1),
            DataReferencia = new DateOnly(2026, 2, 1),
            Quantidade = 12.3m,
            EmissaoCO2 = 4.5m,
        };
        seedContext.Energias.Add(energia);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetEnergiaByIdQueryHandler(dbContext);

        var dto = await handler.Handle(new GetEnergiaByIdQuery(energia.Codigo), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(energia.Codigo, dto!.Codigo);
        Assert.Equal(pessoa.Codigo, dto.CodigoPessoa);
        Assert.Equal(12.3m, dto.Quantidade);
        Assert.Equal(4.5m, dto.EmissaoCO2);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenEnergiaDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new GetEnergiaByIdQueryHandler(dbContext);

        var dto = await handler.Handle(new GetEnergiaByIdQuery(999), CancellationToken.None);

        Assert.Null(dto);
    }
}

public class GetEnergiasQueryTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyEnergiasForGivenPessoa_OrderedByDataReferenciaDescending()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa1 = TestDataFactory.CreatePessoa(seedContext, "p1@example.com");
        var pessoa2 = TestDataFactory.CreatePessoa(seedContext, "p2@example.com");

        seedContext.Energias.AddRange(
            new Energia { CodigoPessoa = pessoa1.Codigo, CriadoEm = new DateOnly(2026, 1, 1), DataReferencia = new DateOnly(2026, 1, 1), Quantidade = 1m, EmissaoCO2 = 0m },
            new Energia { CodigoPessoa = pessoa1.Codigo, CriadoEm = new DateOnly(2026, 1, 1), DataReferencia = new DateOnly(2026, 3, 1), Quantidade = 2m, EmissaoCO2 = 0m },
            new Energia { CodigoPessoa = pessoa2.Codigo, CriadoEm = new DateOnly(2026, 1, 1), DataReferencia = new DateOnly(2026, 2, 1), Quantidade = 3m, EmissaoCO2 = 0m });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetEnergiasQueryHandler(dbContext);

        var results = await handler.Handle(new GetEnergiasQuery(pessoa1.Codigo), CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(pessoa1.Codigo, r.CodigoPessoa));
        Assert.Equal(new DateOnly(2026, 3, 1), results[0].DataReferencia);
        Assert.Equal(new DateOnly(2026, 1, 1), results[1].DataReferencia);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoEnergiasForPessoa()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new GetEnergiasQueryHandler(dbContext);

        var results = await handler.Handle(new GetEnergiasQuery(999), CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Handle_ReturnsAllTimeUnfiltered_WhenNoDateArgsGiven()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);

        seedContext.Energias.AddRange(
            new Energia { CodigoPessoa = pessoa.Codigo, CriadoEm = new DateOnly(2020, 1, 1), DataReferencia = new DateOnly(2020, 1, 1), Quantidade = 1m, EmissaoCO2 = 0m },
            new Energia { CodigoPessoa = pessoa.Codigo, CriadoEm = new DateOnly(2030, 1, 1), DataReferencia = new DateOnly(2030, 1, 1), Quantidade = 2m, EmissaoCO2 = 0m });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetEnergiasQueryHandler(dbContext);

        var results = await handler.Handle(new GetEnergiasQuery(pessoa.Codigo), CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Handle_WithDateRange_ReturnsOnlyRecordsWithinRange_InclusiveOfBoundaries()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);

        var inicio = new DateOnly(2026, 2, 1);
        var fim = new DateOnly(2026, 2, 28);

        seedContext.Energias.AddRange(
            new Energia { CodigoPessoa = pessoa.Codigo, CriadoEm = inicio, DataReferencia = inicio.AddDays(-1), Quantidade = 1m, EmissaoCO2 = 0m }, // just before range
            new Energia { CodigoPessoa = pessoa.Codigo, CriadoEm = inicio, DataReferencia = inicio, Quantidade = 2m, EmissaoCO2 = 0m }, // exactly on DataInicio
            new Energia { CodigoPessoa = pessoa.Codigo, CriadoEm = inicio, DataReferencia = new DateOnly(2026, 2, 15), Quantidade = 3m, EmissaoCO2 = 0m }, // inside range
            new Energia { CodigoPessoa = pessoa.Codigo, CriadoEm = inicio, DataReferencia = fim, Quantidade = 4m, EmissaoCO2 = 0m }, // exactly on DataFim
            new Energia { CodigoPessoa = pessoa.Codigo, CriadoEm = inicio, DataReferencia = fim.AddDays(1), Quantidade = 5m, EmissaoCO2 = 0m }); // just after range
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetEnergiasQueryHandler(dbContext);

        var results = await handler.Handle(new GetEnergiasQuery(pessoa.Codigo, inicio, fim), CancellationToken.None);

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.InRange(r.DataReferencia.ToDateTime(TimeOnly.MinValue), inicio.ToDateTime(TimeOnly.MinValue), fim.ToDateTime(TimeOnly.MinValue)));
    }
}
