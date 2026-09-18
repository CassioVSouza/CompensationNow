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
}
