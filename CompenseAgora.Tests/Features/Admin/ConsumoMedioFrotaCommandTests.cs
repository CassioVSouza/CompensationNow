using CompenseAgora.Common.Exceptions;
using CompenseAgora.Features.ConsumosMedioFrota.Commands.CreateConsumoMedioFrota;
using CompenseAgora.Features.ConsumosMedioFrota.Commands.DeleteConsumoMedioFrota;
using CompenseAgora.Features.ConsumosMedioFrota.Commands.UpdateConsumoMedioFrota;
using CompenseAgora.Features.ConsumosMedioFrota.Queries.GetConsumosMedioFrota;
using CompenseAgora.Tests.Infrastructure;

namespace CompenseAgora.Tests.Features.Admin;

public class ConsumoMedioFrotaCommandTests
{
    [Fact]
    public async Task Create_Update_Delete_RoundTrip()
    {
        using var factory = new SqliteDbContextFactory();

        await using var seedContext = factory.CreateContext();
        var frota = TestDataFactory.CreateFrota(seedContext);

        await using var createContext = factory.CreateContext();
        var codigo = await new CreateConsumoMedioFrotaCommandHandler(createContext).Handle(
            new CreateConsumoMedioFrotaCommand(frota.Codigo, false, 2026, 10m, "km/L"), CancellationToken.None);
        Assert.True(codigo > 0);

        await using var updateContext = factory.CreateContext();
        await new UpdateConsumoMedioFrotaCommandHandler(updateContext).Handle(
            new UpdateConsumoMedioFrotaCommand(codigo, frota.Codigo, true, 2026, 12.5m, "km/L"), CancellationToken.None);

        await using var listContext = factory.CreateContext();
        var lista = await new GetConsumosMedioFrotaQueryHandler(listContext)
            .Handle(new GetConsumosMedioFrotaQuery(), CancellationToken.None);
        var atualizado = Assert.Single(lista);
        Assert.True(atualizado.ParaTodos);
        Assert.Equal(12.5m, atualizado.Consumo);

        await using var deleteContext = factory.CreateContext();
        await new DeleteConsumoMedioFrotaCommandHandler(deleteContext)
            .Handle(new DeleteConsumoMedioFrotaCommand(codigo), CancellationToken.None);

        await using var verifyContext = factory.CreateContext();
        Assert.Null(await verifyContext.ConsumosMedioFrota.FindAsync(codigo));
    }

    [Fact]
    public async Task Delete_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new DeleteConsumoMedioFrotaCommandHandler(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteConsumoMedioFrotaCommand(999), CancellationToken.None));
    }
}
