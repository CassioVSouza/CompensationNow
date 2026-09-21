using CompenseAgora.Entities;
using CompenseAgora.Features.Compensacoes.Queries.GetCompensacaoById;
using CompenseAgora.Features.Compensacoes.Queries.GetCompensacoes;
using CompenseAgora.Tests.Infrastructure;

namespace CompenseAgora.Tests.Features.Compensacoes;

public class GetCompensacaoByIdQueryTests
{
    [Fact]
    public async Task Handle_ReturnsDto_WhenCompensacaoExists()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var compensacao = new Compensacao
        {
            CodigoPessoa = pessoa.Codigo,
            CriadoEm = new DateOnly(2026, 1, 1),
            DataReferencia = new DateOnly(2026, 2, 1),
            TipoCompensacao = "Reflorestamento",
            QuantidadeCompensada = 4.5m,
        };
        seedContext.Compensacoes.Add(compensacao);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetCompensacaoByIdQueryHandler(dbContext);

        var dto = await handler.Handle(new GetCompensacaoByIdQuery(compensacao.Codigo), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(compensacao.Codigo, dto!.Codigo);
        Assert.Equal(pessoa.Codigo, dto.CodigoPessoa);
        Assert.Equal("Reflorestamento", dto.TipoCompensacao);
        Assert.Equal(4.5m, dto.QuantidadeCompensada);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenCompensacaoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new GetCompensacaoByIdQueryHandler(dbContext);

        var dto = await handler.Handle(new GetCompensacaoByIdQuery(999), CancellationToken.None);

        Assert.Null(dto);
    }
}

public class GetCompensacoesQueryTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyCompensacoesForGivenPessoa_OrderedByDataReferenciaDescending()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa1 = TestDataFactory.CreatePessoa(seedContext, "p1@example.com");
        var pessoa2 = TestDataFactory.CreatePessoa(seedContext, "p2@example.com");

        seedContext.Compensacoes.AddRange(
            new Compensacao { CodigoPessoa = pessoa1.Codigo, CriadoEm = new DateOnly(2026, 1, 1), DataReferencia = new DateOnly(2026, 1, 1), TipoCompensacao = "Outro", QuantidadeCompensada = 1m },
            new Compensacao { CodigoPessoa = pessoa1.Codigo, CriadoEm = new DateOnly(2026, 1, 1), DataReferencia = new DateOnly(2026, 3, 1), TipoCompensacao = "Outro", QuantidadeCompensada = 2m },
            new Compensacao { CodigoPessoa = pessoa2.Codigo, CriadoEm = new DateOnly(2026, 1, 1), DataReferencia = new DateOnly(2026, 2, 1), TipoCompensacao = "Outro", QuantidadeCompensada = 3m });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetCompensacoesQueryHandler(dbContext);

        var results = await handler.Handle(new GetCompensacoesQuery(pessoa1.Codigo), CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(pessoa1.Codigo, r.CodigoPessoa));
        Assert.Equal(new DateOnly(2026, 3, 1), results[0].DataReferencia);
        Assert.Equal(new DateOnly(2026, 1, 1), results[1].DataReferencia);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoCompensacoesForPessoa()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new GetCompensacoesQueryHandler(dbContext);

        var results = await handler.Handle(new GetCompensacoesQuery(999), CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Handle_ReturnsAllTimeUnfiltered_WhenNoDateArgsGiven()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);

        seedContext.Compensacoes.AddRange(
            new Compensacao { CodigoPessoa = pessoa.Codigo, CriadoEm = new DateOnly(2020, 1, 1), DataReferencia = new DateOnly(2020, 1, 1), TipoCompensacao = "Outro", QuantidadeCompensada = 1m },
            new Compensacao { CodigoPessoa = pessoa.Codigo, CriadoEm = new DateOnly(2030, 1, 1), DataReferencia = new DateOnly(2030, 1, 1), TipoCompensacao = "Outro", QuantidadeCompensada = 2m });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetCompensacoesQueryHandler(dbContext);

        var results = await handler.Handle(new GetCompensacoesQuery(pessoa.Codigo), CancellationToken.None);

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

        seedContext.Compensacoes.AddRange(
            new Compensacao { CodigoPessoa = pessoa.Codigo, CriadoEm = inicio, DataReferencia = inicio.AddDays(-1), TipoCompensacao = "Outro", QuantidadeCompensada = 1m }, // just before range
            new Compensacao { CodigoPessoa = pessoa.Codigo, CriadoEm = inicio, DataReferencia = inicio, TipoCompensacao = "Outro", QuantidadeCompensada = 2m }, // exactly on DataInicio
            new Compensacao { CodigoPessoa = pessoa.Codigo, CriadoEm = inicio, DataReferencia = new DateOnly(2026, 2, 15), TipoCompensacao = "Outro", QuantidadeCompensada = 3m }, // inside range
            new Compensacao { CodigoPessoa = pessoa.Codigo, CriadoEm = inicio, DataReferencia = fim, TipoCompensacao = "Outro", QuantidadeCompensada = 4m }, // exactly on DataFim
            new Compensacao { CodigoPessoa = pessoa.Codigo, CriadoEm = inicio, DataReferencia = fim.AddDays(1), TipoCompensacao = "Outro", QuantidadeCompensada = 5m }); // just after range
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new GetCompensacoesQueryHandler(dbContext);

        var results = await handler.Handle(new GetCompensacoesQuery(pessoa.Codigo, inicio, fim), CancellationToken.None);

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.InRange(r.DataReferencia.ToDateTime(TimeOnly.MinValue), inicio.ToDateTime(TimeOnly.MinValue), fim.ToDateTime(TimeOnly.MinValue)));
    }
}
