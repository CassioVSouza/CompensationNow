using CompenseAgora.Common.Exceptions;
using CompenseAgora.Features.FatoresCombustivel.Commands.CreateFatorCombustivel;
using CompenseAgora.Features.FatoresCombustivel.Commands.DeleteFatorCombustivel;
using CompenseAgora.Features.FatoresCombustivel.Commands.UpdateFatorCombustivel;
using CompenseAgora.Features.FatoresCombustivel.Queries.GetFatoresCombustivel;
using CompenseAgora.Tests.Infrastructure;

namespace CompenseAgora.Tests.Features.Admin;

public class FatorCombustivelCommandTests
{
    [Fact]
    public async Task Create_Update_Delete_RoundTrip()
    {
        using var factory = new SqliteDbContextFactory();

        await using var seedContext = factory.CreateContext();
        var diesel = TestDataFactory.CreateCombustivel(seedContext, "Diesel");

        await using var createContext = factory.CreateContext();
        var codigo = await new CreateFatorCombustivelCommandHandler(createContext).Handle(
            new CreateFatorCombustivelCommand(diesel.Codigo, 2026, 10.5m, 0.84m, 2603m, 0.1m, 0.1m),
            CancellationToken.None);
        Assert.True(codigo > 0);

        await using var updateContext = factory.CreateContext();
        await new UpdateFatorCombustivelCommandHandler(updateContext).Handle(
            new UpdateFatorCombustivelCommand(codigo, diesel.Codigo, 2027, 10.5m, 0.84m, 2650m, 0.2m, 0.2m),
            CancellationToken.None);

        await using var listContext = factory.CreateContext();
        var lista = await new GetFatoresCombustivelQueryHandler(listContext)
            .Handle(new GetFatoresCombustivelQuery(), CancellationToken.None);
        var atualizado = Assert.Single(lista);
        Assert.Equal(2027, atualizado.Ano);
        Assert.Equal(2650m, atualizado.CO2);
        Assert.Equal("Diesel", atualizado.NomeCombustivel);

        await using var deleteContext = factory.CreateContext();
        await new DeleteFatorCombustivelCommandHandler(deleteContext)
            .Handle(new DeleteFatorCombustivelCommand(codigo), CancellationToken.None);

        await using var verifyContext = factory.CreateContext();
        Assert.Null(await verifyContext.FatoresDoCombustivel.FindAsync(codigo));
    }

    [Fact]
    public async Task Delete_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new DeleteFatorCombustivelCommandHandler(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteFatorCombustivelCommand(999), CancellationToken.None));
    }
}
