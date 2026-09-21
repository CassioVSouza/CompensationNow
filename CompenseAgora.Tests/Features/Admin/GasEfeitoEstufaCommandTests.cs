using CompenseAgora.Common.Exceptions;
using CompenseAgora.Entities;
using CompenseAgora.Features.GasesEfeitoEstufa.Commands.CreateGasEfeitoEstufa;
using CompenseAgora.Features.GasesEfeitoEstufa.Commands.DeleteGasEfeitoEstufa;
using CompenseAgora.Features.GasesEfeitoEstufa.Commands.UpdateGasEfeitoEstufa;
using CompenseAgora.Features.GasesEfeitoEstufa.Queries.GetGasesEfeitoEstufa;
using CompenseAgora.Tests.Infrastructure;

namespace CompenseAgora.Tests.Features.Admin;

public class GasEfeitoEstufaCommandTests
{
    [Fact]
    public async Task Create_Update_Delete_RoundTrip()
    {
        using var factory = new SqliteDbContextFactory();

        await using var createContext = factory.CreateContext();
        var codigo = await new CreateGasEfeitoEstufaCommandHandler(createContext)
            .Handle(new CreateGasEfeitoEstufaCommand("Metano (CH4)", "GEE", 25m), CancellationToken.None);
        Assert.True(codigo > 0);

        await using var updateContext = factory.CreateContext();
        await new UpdateGasEfeitoEstufaCommandHandler(updateContext)
            .Handle(new UpdateGasEfeitoEstufaCommand(codigo, "Metano (CH4)", "GEE", 27.9m), CancellationToken.None);

        await using var listContext = factory.CreateContext();
        var lista = await new GetGasesEfeitoEstufaQueryHandler(listContext)
            .Handle(new GetGasesEfeitoEstufaQuery(), CancellationToken.None);
        Assert.Single(lista);
        Assert.Equal(27.9m, lista[0].GWP);

        await using var deleteContext = factory.CreateContext();
        await new DeleteGasEfeitoEstufaCommandHandler(deleteContext)
            .Handle(new DeleteGasEfeitoEstufaCommand(codigo), CancellationToken.None);

        await using var verifyContext = factory.CreateContext();
        Assert.Null(await verifyContext.GasesEfeitoEstufa.FindAsync(codigo));
    }

    [Fact]
    public async Task Update_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new UpdateGasEfeitoEstufaCommandHandler(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateGasEfeitoEstufaCommand(999, "X", "Y", 1m), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new DeleteGasEfeitoEstufaCommandHandler(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteGasEfeitoEstufaCommand(999), CancellationToken.None));
    }
}
