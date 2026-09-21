using CompenseAgora.Common.Exceptions;
using CompenseAgora.Features.Combustiveis.Commands.CreateCombustivel;
using CompenseAgora.Features.Combustiveis.Commands.DeleteCombustivel;
using CompenseAgora.Features.Combustiveis.Commands.UpdateCombustivel;
using CompenseAgora.Features.Combustiveis.Queries.GetTodosCombustiveis;
using CompenseAgora.Tests.Infrastructure;

namespace CompenseAgora.Tests.Features.Admin;

public class CombustivelCommandTests
{
    [Fact]
    public async Task Create_Update_Delete_RoundTrip_WithSelfReferences()
    {
        using var factory = new SqliteDbContextFactory();

        await using var seedContext = factory.CreateContext();
        var etanol = TestDataFactory.CreateCombustivel(seedContext, "Etanol");
        var gasolinaFossil = TestDataFactory.CreateCombustivel(seedContext, "Gasolina A");

        await using var createContext = factory.CreateContext();
        var codigo = await new CreateCombustivelCommandHandler(createContext).Handle(
            new CreateCombustivelCommand("Gasolina C", "L", false, etanol.Codigo, gasolinaFossil.Codigo),
            CancellationToken.None);
        Assert.True(codigo > 0);

        await using var updateContext = factory.CreateContext();
        await new UpdateCombustivelCommandHandler(updateContext).Handle(
            new UpdateCombustivelCommand(codigo, "Gasolina Comum", "L", false, etanol.Codigo, gasolinaFossil.Codigo),
            CancellationToken.None);

        await using var listContext = factory.CreateContext();
        var lista = await new GetTodosCombustiveisQueryHandler(listContext)
            .Handle(new GetTodosCombustiveisQuery(), CancellationToken.None);
        var atualizado = Assert.Single(lista, c => c.Codigo == codigo);
        Assert.Equal("Gasolina Comum", atualizado.Nome);
        Assert.Equal("Etanol", atualizado.NomeCombustivelBiogenico);
        Assert.Equal("Gasolina A", atualizado.NomeCombustivelFossil);

        await using var deleteContext = factory.CreateContext();
        await new DeleteCombustivelCommandHandler(deleteContext)
            .Handle(new DeleteCombustivelCommand(codigo), CancellationToken.None);

        await using var verifyContext = factory.CreateContext();
        Assert.Null(await verifyContext.Combustiveis.FindAsync(codigo));
    }

    [Fact]
    public async Task Delete_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new DeleteCombustivelCommandHandler(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteCombustivelCommand(999), CancellationToken.None));
    }
}
