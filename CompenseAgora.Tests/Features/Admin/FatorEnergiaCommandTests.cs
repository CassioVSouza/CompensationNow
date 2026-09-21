using CompenseAgora.Common.Exceptions;
using CompenseAgora.Features.FatoresEnergia.Commands.CreateFatorEnergia;
using CompenseAgora.Features.FatoresEnergia.Commands.DeleteFatorEnergia;
using CompenseAgora.Features.FatoresEnergia.Commands.UpdateFatorEnergia;
using CompenseAgora.Features.FatoresEnergia.Queries.GetFatoresEnergia;
using CompenseAgora.Tests.Infrastructure;

namespace CompenseAgora.Tests.Features.Admin;

public class FatorEnergiaCommandTests
{
    [Fact]
    public async Task Create_Update_Delete_RoundTrip()
    {
        using var factory = new SqliteDbContextFactory();

        await using var createContext = factory.CreateContext();
        var codigo = await new CreateFatorEnergiaCommandHandler(createContext)
            .Handle(new CreateFatorEnergiaCommand(6, 2026, 0.08m), CancellationToken.None);
        Assert.True(codigo > 0);

        await using var updateContext = factory.CreateContext();
        await new UpdateFatorEnergiaCommandHandler(updateContext)
            .Handle(new UpdateFatorEnergiaCommand(codigo, 7, 2026, 0.09m), CancellationToken.None);

        await using var listContext = factory.CreateContext();
        var lista = await new GetFatoresEnergiaQueryHandler(listContext)
            .Handle(new GetFatoresEnergiaQuery(), CancellationToken.None);
        var atualizado = Assert.Single(lista);
        Assert.Equal(7, atualizado.Mes);
        Assert.Equal(0.09m, atualizado.FeSin);

        await using var deleteContext = factory.CreateContext();
        await new DeleteFatorEnergiaCommandHandler(deleteContext)
            .Handle(new DeleteFatorEnergiaCommand(codigo), CancellationToken.None);

        await using var verifyContext = factory.CreateContext();
        Assert.Null(await verifyContext.FatoresEnergia.FindAsync(codigo));
    }

    [Fact]
    public async Task Delete_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new DeleteFatorEnergiaCommandHandler(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteFatorEnergiaCommand(999), CancellationToken.None));
    }
}
