using CompenseAgora.Common.Exceptions;
using CompenseAgora.Entities;
using CompenseAgora.Features.Energias.Calculo;
using CompenseAgora.Features.Energias.Commands.DeleteEnergia;
using CompenseAgora.Features.Energias.Commands.UpdateEnergia;
using CompenseAgora.Tests.Infrastructure;
using FluentValidation.TestHelper;

namespace CompenseAgora.Tests.Features.Energias;

public class UpdateEnergiaCommandValidatorTests
{
    private readonly UpdateEnergiaCommandValidator _validator = new();

    private static UpdateEnergiaCommand ValidCommand() => new(
        Codigo: 1,
        DataReferencia: new DateOnly(2026, 1, 1),
        Quantidade: 10m);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Codigo_Zero_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { Codigo = 0 });
        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }

    [Fact]
    public void DataReferencia_Default_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { DataReferencia = default });
        result.ShouldHaveValidationErrorFor(x => x.DataReferencia);
    }

    [Fact]
    public void Quantidade_Zero_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { Quantidade = 0 });
        result.ShouldHaveValidationErrorFor(x => x.Quantidade);
    }
}

public class UpdateEnergiaCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesDataReferenciaAndQuantidade()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var energia = new Energia
        {
            CodigoPessoa = pessoa.Codigo,
            CriadoEm = new DateOnly(2026, 1, 1),
            DataReferencia = new DateOnly(2026, 1, 1),
            Quantidade = 5m,
            EmissaoCO2 = 0m,
        };
        seedContext.Energias.Add(energia);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new UpdateEnergiaCommandHandler(dbContext, new CalculadoraEmissaoEnergia(dbContext));

        var command = new UpdateEnergiaCommand(energia.Codigo, new DateOnly(2026, 6, 15), 99.9m);
        await handler.Handle(command, CancellationToken.None);

        var updated = await dbContext.Energias.FindAsync(energia.Codigo);
        Assert.NotNull(updated);
        Assert.Equal(new DateOnly(2026, 6, 15), updated!.DataReferencia);
        Assert.Equal(99.9m, updated.Quantidade);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new UpdateEnergiaCommandHandler(dbContext, new CalculadoraEmissaoEnergia(dbContext));

        var command = new UpdateEnergiaCommand(999, new DateOnly(2026, 1, 1), 10m);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }
}

public class DeleteEnergiaCommandValidatorTests
{
    private readonly DeleteEnergiaCommandValidator _validator = new();

    [Fact]
    public void Codigo_Positive_Passes()
    {
        var result = _validator.TestValidate(new DeleteEnergiaCommand(1));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Codigo_Zero_Fails()
    {
        var result = _validator.TestValidate(new DeleteEnergiaCommand(0));
        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }
}

public class DeleteEnergiaCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemovesEnergia_WhenItExists()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var energia = new Energia
        {
            CodigoPessoa = pessoa.Codigo,
            CriadoEm = new DateOnly(2026, 1, 1),
            DataReferencia = new DateOnly(2026, 1, 1),
            Quantidade = 5m,
            EmissaoCO2 = 0m,
        };
        seedContext.Energias.Add(energia);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new DeleteEnergiaCommandHandler(dbContext);

        await handler.Handle(new DeleteEnergiaCommand(energia.Codigo), CancellationToken.None);

        await using var verifyContext = factory.CreateContext();
        Assert.Null(await verifyContext.Energias.FindAsync(energia.Codigo));
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new DeleteEnergiaCommandHandler(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new DeleteEnergiaCommand(999), CancellationToken.None));
    }
}
