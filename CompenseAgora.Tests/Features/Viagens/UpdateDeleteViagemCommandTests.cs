using CompenseAgora.Common.Exceptions;
using CompenseAgora.Entities;
using CompenseAgora.Features.Viagens.Calculo;
using CompenseAgora.Features.Viagens.Commands.DeleteViagem;
using CompenseAgora.Features.Viagens.Commands.UpdateViagem;
using CompenseAgora.Tests.Infrastructure;
using FluentValidation.TestHelper;

namespace CompenseAgora.Tests.Features.Viagens;

public class UpdateViagemCommandValidatorTests
{
    private readonly UpdateViagemCommandValidator _validator = new();

    private static UpdateViagemCommand ValidCommand() => new(
        Codigo: 1,
        CodigoFrota: 1,
        CodigoCombustivel: null,
        DataReferencia: new DateOnly(2026, 1, 1),
        Consumo: 10m,
        AnoFrota: 2020,
        DistanciaKM: 100m);

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
    public void CodigoFrota_Zero_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { CodigoFrota = 0 });
        result.ShouldHaveValidationErrorFor(x => x.CodigoFrota);
    }

    [Fact]
    public void CodigoCombustivel_Negative_Fails_WhenProvided()
    {
        var result = _validator.TestValidate(ValidCommand() with { CodigoCombustivel = -1 });
        result.ShouldHaveValidationErrorFor(x => x.CodigoCombustivel);
    }
}

public class UpdateViagemCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesFields_IncludingClearingCombustivelToNull()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var combustivel = TestDataFactory.CreateCombustivel(seedContext);
        var frota1 = TestDataFactory.CreateFrota(seedContext, combustivel, "Frota A");
        var frota2 = TestDataFactory.CreateFrota(seedContext, combustivel, "Frota B");

        var viagem = new Viagem
        {
            CodigoPessoa = pessoa.Codigo,
            CodigoFrota = frota1.Codigo,
            CodigoCombustivel = combustivel.Codigo,
            CriadoEm = new DateOnly(2026, 1, 1),
            DataReferencia = new DateOnly(2026, 1, 1),
            Consumo = 5m,
            AnoFrota = 2019,
            DistanciaKM = 50m,
            EmissaoCO2 = 0m,
        };
        seedContext.Viagens.Add(viagem);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new UpdateViagemCommandHandler(dbContext, new CalculadoraEmissaoViagem(dbContext));

        var command = new UpdateViagemCommand(
            viagem.Codigo, frota2.Codigo, CodigoCombustivel: null,
            DataReferencia: new DateOnly(2026, 5, 5), Consumo: 20m, AnoFrota: 2022, DistanciaKM: 300m);
        await handler.Handle(command, CancellationToken.None);

        var updated = await dbContext.Viagens.FindAsync(viagem.Codigo);
        Assert.NotNull(updated);
        Assert.Equal(frota2.Codigo, updated!.CodigoFrota);
        Assert.Null(updated.CodigoCombustivel);
        Assert.Equal(new DateOnly(2026, 5, 5), updated.DataReferencia);
        Assert.Equal(20m, updated.Consumo);
        Assert.Equal(2022, updated.AnoFrota);
        Assert.Equal(300m, updated.DistanciaKM);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new UpdateViagemCommandHandler(dbContext, new CalculadoraEmissaoViagem(dbContext));

        var command = new UpdateViagemCommand(999, 1, null, new DateOnly(2026, 1, 1), 10m, 2020, 50m);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }
}

public class DeleteViagemCommandValidatorTests
{
    private readonly DeleteViagemCommandValidator _validator = new();

    [Fact]
    public void Codigo_Positive_Passes()
    {
        var result = _validator.TestValidate(new DeleteViagemCommand(1));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Codigo_Zero_Fails()
    {
        var result = _validator.TestValidate(new DeleteViagemCommand(0));
        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }
}

public class DeleteViagemCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemovesViagem_WhenItExists()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var frota = TestDataFactory.CreateFrota(seedContext);

        var viagem = new Viagem
        {
            CodigoPessoa = pessoa.Codigo,
            CodigoFrota = frota.Codigo,
            CodigoCombustivel = null,
            CriadoEm = new DateOnly(2026, 1, 1),
            DataReferencia = new DateOnly(2026, 1, 1),
            Consumo = 5m,
            AnoFrota = 2019,
            DistanciaKM = 50m,
            EmissaoCO2 = 0m,
        };
        seedContext.Viagens.Add(viagem);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new DeleteViagemCommandHandler(dbContext);

        await handler.Handle(new DeleteViagemCommand(viagem.Codigo), CancellationToken.None);

        await using var verifyContext = factory.CreateContext();
        Assert.Null(await verifyContext.Viagens.FindAsync(viagem.Codigo));
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new DeleteViagemCommandHandler(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new DeleteViagemCommand(999), CancellationToken.None));
    }
}
