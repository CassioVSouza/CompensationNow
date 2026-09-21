using CompenseAgora.Common.Exceptions;
using CompenseAgora.Features.Frotas.Commands.CreateFrota;
using CompenseAgora.Features.Frotas.Commands.DeleteFrota;
using CompenseAgora.Features.Frotas.Commands.UpdateFrota;
using CompenseAgora.Features.Frotas.Queries.GetFrotas;
using CompenseAgora.Tests.Infrastructure;

namespace CompenseAgora.Tests.Features.Admin;

public class FrotaCommandTests
{
    [Fact]
    public async Task Create_Update_Delete_RoundTrip()
    {
        using var factory = new SqliteDbContextFactory();

        await using var seedContext = factory.CreateContext();
        var gasolina = TestDataFactory.CreateCombustivel(seedContext, "Gasolina");
        var etanol = TestDataFactory.CreateCombustivel(seedContext, "Etanol");

        await using var createContext = factory.CreateContext();
        var codigo = await new CreateFrotaCommandHandler(createContext).Handle(
            new CreateFrotaCommand("Flex", gasolina.Codigo, etanol.Codigo, gasolina.Codigo), CancellationToken.None);
        Assert.True(codigo > 0);

        await using var updateContext = factory.CreateContext();
        await new UpdateFrotaCommandHandler(updateContext).Handle(
            new UpdateFrotaCommand(codigo, "Flex Atualizada", gasolina.Codigo, etanol.Codigo, gasolina.Codigo),
            CancellationToken.None);

        await using var listContext = factory.CreateContext();
        var lista = await new GetFrotasQueryHandler(listContext).Handle(new GetFrotasQuery(), CancellationToken.None);
        var atualizada = Assert.Single(lista);
        Assert.Equal("Flex Atualizada", atualizada.Nome);
        Assert.Equal("Etanol", atualizada.NomeCombustivelBiogenico);

        await using var deleteContext = factory.CreateContext();
        await new DeleteFrotaCommandHandler(deleteContext).Handle(new DeleteFrotaCommand(codigo), CancellationToken.None);

        await using var verifyContext = factory.CreateContext();
        Assert.Null(await verifyContext.Frotas.FindAsync(codigo));
    }

    [Fact]
    public async Task Delete_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new DeleteFrotaCommandHandler(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteFrotaCommand(999), CancellationToken.None));
    }
}
