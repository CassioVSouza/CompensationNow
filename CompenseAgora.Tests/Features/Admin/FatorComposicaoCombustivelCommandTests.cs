using CompenseAgora.Common.Exceptions;
using CompenseAgora.Features.FatoresComposicaoCombustivel.Commands.CreateFatorComposicaoCombustivel;
using CompenseAgora.Features.FatoresComposicaoCombustivel.Commands.DeleteFatorComposicaoCombustivel;
using CompenseAgora.Features.FatoresComposicaoCombustivel.Commands.UpdateFatorComposicaoCombustivel;
using CompenseAgora.Features.FatoresComposicaoCombustivel.Queries.GetFatoresComposicaoCombustivel;
using CompenseAgora.Tests.Infrastructure;

namespace CompenseAgora.Tests.Features.Admin;

public class FatorComposicaoCombustivelCommandTests
{
    [Fact]
    public async Task Create_Update_Delete_RoundTrip()
    {
        using var factory = new SqliteDbContextFactory();

        await using var createContext = factory.CreateContext();
        var codigo = await new CreateFatorComposicaoCombustivelCommandHandler(createContext).Handle(
            new CreateFatorComposicaoCombustivelCommand(6, 2026, 27m, 12m), CancellationToken.None);
        Assert.True(codigo > 0);

        await using var updateContext = factory.CreateContext();
        await new UpdateFatorComposicaoCombustivelCommandHandler(updateContext).Handle(
            new UpdateFatorComposicaoCombustivelCommand(codigo, 7, 2026, 30m, 14m), CancellationToken.None);

        await using var listContext = factory.CreateContext();
        var lista = await new GetFatoresComposicaoCombustivelQueryHandler(listContext)
            .Handle(new GetFatoresComposicaoCombustivelQuery(), CancellationToken.None);
        var atualizado = Assert.Single(lista);
        Assert.Equal(7, atualizado.Mes);
        Assert.Equal(30m, atualizado.PercentualEtanol);

        await using var deleteContext = factory.CreateContext();
        await new DeleteFatorComposicaoCombustivelCommandHandler(deleteContext)
            .Handle(new DeleteFatorComposicaoCombustivelCommand(codigo), CancellationToken.None);

        await using var verifyContext = factory.CreateContext();
        Assert.Null(await verifyContext.FatoresComposicaoCombustivel.FindAsync(codigo));
    }

    [Fact]
    public async Task Delete_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new DeleteFatorComposicaoCombustivelCommandHandler(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteFatorComposicaoCombustivelCommand(999), CancellationToken.None));
    }
}
