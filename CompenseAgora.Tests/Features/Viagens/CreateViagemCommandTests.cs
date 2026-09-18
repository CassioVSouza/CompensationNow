using CompenseAgora.Features.Viagens.Calculo;
using CompenseAgora.Features.Viagens.Commands.CreateViagem;
using CompenseAgora.Tests.Infrastructure;
using FluentValidation.TestHelper;

namespace CompenseAgora.Tests.Features.Viagens;

public class CreateViagemCommandValidatorTests
{
    private readonly CreateViagemCommandValidator _validator = new();

    private static CreateViagemCommand ValidCommand() => new(
        CodigoPessoa: 1,
        CodigoFrota: 1,
        CodigoCombustivel: null,
        DataReferencia: new DateOnly(2026, 1, 1),
        Consumo: 10m,
        AnoFrota: 2020,
        DistanciaKM: 100m);

    [Fact]
    public void Valid_Command_WithNullCombustivel_Passes()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valid_Command_WithPositiveCombustivel_Passes()
    {
        var result = _validator.TestValidate(ValidCommand() with { CodigoCombustivel = 3 });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CodigoPessoa_Zero_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { CodigoPessoa = 0 });
        result.ShouldHaveValidationErrorFor(x => x.CodigoPessoa);
    }

    [Fact]
    public void CodigoFrota_Zero_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { CodigoFrota = 0 });
        result.ShouldHaveValidationErrorFor(x => x.CodigoFrota);
    }

    [Fact]
    public void CodigoCombustivel_ZeroOrNegative_Fails_WhenProvided()
    {
        var result = _validator.TestValidate(ValidCommand() with { CodigoCombustivel = 0 });
        result.ShouldHaveValidationErrorFor(x => x.CodigoCombustivel);
    }

    [Fact]
    public void DataReferencia_Default_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { DataReferencia = default });
        result.ShouldHaveValidationErrorFor(x => x.DataReferencia);
    }

    [Fact]
    public void Consumo_Negative_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { Consumo = -1 });
        result.ShouldHaveValidationErrorFor(x => x.Consumo);
    }

    [Fact]
    public void Consumo_Zero_Passes()
    {
        var result = _validator.TestValidate(ValidCommand() with { Consumo = 0 });
        result.ShouldNotHaveValidationErrorFor(x => x.Consumo);
    }

    [Fact]
    public void AnoFrota_Negative_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { AnoFrota = -1 });
        result.ShouldHaveValidationErrorFor(x => x.AnoFrota);
    }

    [Fact]
    public void DistanciaKM_Negative_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { DistanciaKM = -1 });
        result.ShouldHaveValidationErrorFor(x => x.DistanciaKM);
    }
}

public class CreateViagemCommandHandlerTests
{
    [Fact]
    public async Task Handle_InsertsViagem_WithNullCombustivel_AndZeroEmissao()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var frota = TestDataFactory.CreateFrota(seedContext);

        await using var dbContext = factory.CreateContext();
        var handler = new CreateViagemCommandHandler(dbContext, new CalculadoraEmissaoViagem(dbContext));

        var command = new CreateViagemCommand(
            pessoa.Codigo, frota.Codigo, CodigoCombustivel: null,
            DataReferencia: new DateOnly(2026, 4, 1), Consumo: 15m, AnoFrota: 2021, DistanciaKM: 200m);

        var codigo = await handler.Handle(command, CancellationToken.None);

        Assert.True(codigo > 0);

        var viagem = await dbContext.Viagens.FindAsync(codigo);
        Assert.NotNull(viagem);
        Assert.Null(viagem!.CodigoCombustivel);
        Assert.Equal(0m, viagem.EmissaoCO2);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), viagem.CriadoEm);
    }

    [Fact]
    public async Task Handle_InsertsViagem_WithCombustivelSet()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        var combustivel = TestDataFactory.CreateCombustivel(seedContext, "Etanol");
        var frota = TestDataFactory.CreateFrota(seedContext, combustivel);

        await using var dbContext = factory.CreateContext();
        var handler = new CreateViagemCommandHandler(dbContext, new CalculadoraEmissaoViagem(dbContext));

        var command = new CreateViagemCommand(
            pessoa.Codigo, frota.Codigo, CodigoCombustivel: combustivel.Codigo,
            DataReferencia: new DateOnly(2026, 4, 1), Consumo: 15m, AnoFrota: 2021, DistanciaKM: 200m);

        var codigo = await handler.Handle(command, CancellationToken.None);

        var viagem = await dbContext.Viagens.FindAsync(codigo);
        Assert.NotNull(viagem);
        Assert.Equal(combustivel.Codigo, viagem!.CodigoCombustivel);
    }
}
