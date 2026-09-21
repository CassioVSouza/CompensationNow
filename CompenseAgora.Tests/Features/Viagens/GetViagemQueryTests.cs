using CompenseAgora.Entities;
using CompenseAgora.Features.Viagens.Queries.GetViagemById;
using CompenseAgora.Features.Viagens.Queries.GetViagens;
using CompenseAgora.Tests.Infrastructure;

namespace CompenseAgora.Tests.Features.Viagens;

public class GetViagemByIdQueryTests
{
    [Fact]
    public async Task Handle_ReturnsDto_WithFrotaAndCombustivelNames_WhenCombustivelSet()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var combustivel = TestDataFactory.CreateCombustivel(seedContext, "Gasolina");
        var frota = TestDataFactory.CreateFrota(seedContext, combustivel, "Frota Centro");

        var viagem = new Viagem
        {
            CodigoPessoa = pessoa.Codigo,
            CodigoFrota = frota.Codigo,
            CodigoCombustivel = combustivel.Codigo,
            CriadoEm = new DateOnly(2026, 1, 1),
            DataReferencia = new DateOnly(2026, 2, 2),
            Consumo = 10m,
            AnoFrota = 2020,
            DistanciaKM = 150m,
            EmissaoCO2 = 3m,
        };
        seedContext.Viagens.Add(viagem);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetViagemByIdQueryHandler(dbContext);

        var dto = await handler.Handle(new GetViagemByIdQuery(viagem.Codigo), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("Frota Centro", dto!.FrotaNome);
        Assert.Equal("Gasolina", dto.CombustivelNome);
        Assert.Equal(combustivel.Codigo, dto.CodigoCombustivel);
    }

    [Fact]
    public async Task Handle_ReturnsDto_WithNullCombustivelNome_WhenCombustivelNotSet()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var frota = TestDataFactory.CreateFrota(seedContext, nome: "Frota Norte");

        var viagem = new Viagem
        {
            CodigoPessoa = pessoa.Codigo,
            CodigoFrota = frota.Codigo,
            CodigoCombustivel = null,
            CriadoEm = new DateOnly(2026, 1, 1),
            DataReferencia = new DateOnly(2026, 2, 2),
            Consumo = 10m,
            AnoFrota = 2020,
            DistanciaKM = 150m,
            EmissaoCO2 = 0m,
        };
        seedContext.Viagens.Add(viagem);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetViagemByIdQueryHandler(dbContext);

        var dto = await handler.Handle(new GetViagemByIdQuery(viagem.Codigo), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("Frota Norte", dto!.FrotaNome);
        Assert.Null(dto.CombustivelNome);
        Assert.Null(dto.CodigoCombustivel);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenViagemDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new GetViagemByIdQueryHandler(dbContext);

        var dto = await handler.Handle(new GetViagemByIdQuery(999), CancellationToken.None);

        Assert.Null(dto);
    }
}

public class GetViagensQueryTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyViagensForGivenPessoa_OrderedByDataReferenciaDescending()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa1 = TestDataFactory.CreatePessoa(seedContext, "p1@example.com");
        var pessoa2 = TestDataFactory.CreatePessoa(seedContext, "p2@example.com");
        var frota = TestDataFactory.CreateFrota(seedContext);

        seedContext.Viagens.AddRange(
            new Viagem { CodigoPessoa = pessoa1.Codigo, CodigoFrota = frota.Codigo, CriadoEm = new DateOnly(2026, 1, 1), DataReferencia = new DateOnly(2026, 1, 1), Consumo = 1m, AnoFrota = 2020, DistanciaKM = 10m, EmissaoCO2 = 0m },
            new Viagem { CodigoPessoa = pessoa1.Codigo, CodigoFrota = frota.Codigo, CriadoEm = new DateOnly(2026, 1, 1), DataReferencia = new DateOnly(2026, 3, 1), Consumo = 2m, AnoFrota = 2020, DistanciaKM = 20m, EmissaoCO2 = 0m },
            new Viagem { CodigoPessoa = pessoa2.Codigo, CodigoFrota = frota.Codigo, CriadoEm = new DateOnly(2026, 1, 1), DataReferencia = new DateOnly(2026, 2, 1), Consumo = 3m, AnoFrota = 2020, DistanciaKM = 30m, EmissaoCO2 = 0m });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetViagensQueryHandler(dbContext);

        var results = await handler.Handle(new GetViagensQuery(pessoa1.Codigo), CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(pessoa1.Codigo, r.CodigoPessoa));
        Assert.Equal(new DateOnly(2026, 3, 1), results[0].DataReferencia);
        Assert.Equal(new DateOnly(2026, 1, 1), results[1].DataReferencia);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoViagensForPessoa()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new GetViagensQueryHandler(dbContext);

        var results = await handler.Handle(new GetViagensQuery(999), CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Handle_ReturnsAllTimeUnfiltered_WhenNoDateArgsGiven()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var frota = TestDataFactory.CreateFrota(seedContext);

        seedContext.Viagens.AddRange(
            new Viagem { CodigoPessoa = pessoa.Codigo, CodigoFrota = frota.Codigo, CriadoEm = new DateOnly(2020, 1, 1), DataReferencia = new DateOnly(2020, 1, 1), Consumo = 1m, AnoFrota = 2020, DistanciaKM = 10m, EmissaoCO2 = 0m },
            new Viagem { CodigoPessoa = pessoa.Codigo, CodigoFrota = frota.Codigo, CriadoEm = new DateOnly(2030, 1, 1), DataReferencia = new DateOnly(2030, 1, 1), Consumo = 2m, AnoFrota = 2020, DistanciaKM = 20m, EmissaoCO2 = 0m });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetViagensQueryHandler(dbContext);

        var results = await handler.Handle(new GetViagensQuery(pessoa.Codigo), CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Handle_WithDateRange_ReturnsOnlyRecordsWithinRange_InclusiveOfBoundaries()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var frota = TestDataFactory.CreateFrota(seedContext);

        var inicio = new DateOnly(2026, 2, 1);
        var fim = new DateOnly(2026, 2, 28);

        seedContext.Viagens.AddRange(
            new Viagem { CodigoPessoa = pessoa.Codigo, CodigoFrota = frota.Codigo, CriadoEm = inicio, DataReferencia = inicio.AddDays(-1), Consumo = 1m, AnoFrota = 2020, DistanciaKM = 10m, EmissaoCO2 = 0m }, // just before range
            new Viagem { CodigoPessoa = pessoa.Codigo, CodigoFrota = frota.Codigo, CriadoEm = inicio, DataReferencia = inicio, Consumo = 2m, AnoFrota = 2020, DistanciaKM = 20m, EmissaoCO2 = 0m }, // exactly on DataInicio
            new Viagem { CodigoPessoa = pessoa.Codigo, CodigoFrota = frota.Codigo, CriadoEm = inicio, DataReferencia = new DateOnly(2026, 2, 15), Consumo = 3m, AnoFrota = 2020, DistanciaKM = 30m, EmissaoCO2 = 0m }, // inside range
            new Viagem { CodigoPessoa = pessoa.Codigo, CodigoFrota = frota.Codigo, CriadoEm = inicio, DataReferencia = fim, Consumo = 4m, AnoFrota = 2020, DistanciaKM = 40m, EmissaoCO2 = 0m }, // exactly on DataFim
            new Viagem { CodigoPessoa = pessoa.Codigo, CodigoFrota = frota.Codigo, CriadoEm = inicio, DataReferencia = fim.AddDays(1), Consumo = 5m, AnoFrota = 2020, DistanciaKM = 50m, EmissaoCO2 = 0m }); // just after range
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetViagensQueryHandler(dbContext);

        var results = await handler.Handle(new GetViagensQuery(pessoa.Codigo, inicio, fim), CancellationToken.None);

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.InRange(r.DataReferencia.ToDateTime(TimeOnly.MinValue), inicio.ToDateTime(TimeOnly.MinValue), fim.ToDateTime(TimeOnly.MinValue)));
    }
}
