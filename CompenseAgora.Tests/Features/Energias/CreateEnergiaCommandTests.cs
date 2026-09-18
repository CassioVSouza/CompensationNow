using CompenseAgora.Features.Energias.Calculo;
using CompenseAgora.Features.Energias.Commands.CreateEnergia;
using CompenseAgora.Tests.Infrastructure;
using FluentValidation.TestHelper;

namespace CompenseAgora.Tests.Features.Energias;

public class CreateEnergiaCommandValidatorTests
{
    private readonly CreateEnergiaCommandValidator _validator = new();

    private static CreateEnergiaCommand ValidCommand() => new(
        CodigoPessoa: 1,
        DataReferencia: new DateOnly(2026, 1, 1),
        Quantidade: 10m);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CodigoPessoa_Zero_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { CodigoPessoa = 0 });
        result.ShouldHaveValidationErrorFor(x => x.CodigoPessoa);
    }

    [Fact]
    public void CodigoPessoa_Negative_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { CodigoPessoa = -1 });
        result.ShouldHaveValidationErrorFor(x => x.CodigoPessoa);
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

    [Fact]
    public void Quantidade_Negative_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { Quantidade = -1 });
        result.ShouldHaveValidationErrorFor(x => x.Quantidade);
    }
}

public class CreateEnergiaCommandHandlerTests
{
    [Fact]
    public async Task Handle_InsertsEnergia_WithCriadoEmToday_AndZeroEmissao()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);

        await using var dbContext = factory.CreateContext();
        var handler = new CreateEnergiaCommandHandler(dbContext, new CalculadoraEmissaoEnergia(dbContext));

        var command = new CreateEnergiaCommand(pessoa.Codigo, new DateOnly(2026, 3, 1), 42.5m);

        var codigo = await handler.Handle(command, CancellationToken.None);

        Assert.True(codigo > 0);

        var energia = await dbContext.Energias.FindAsync(codigo);
        Assert.NotNull(energia);
        Assert.Equal(pessoa.Codigo, energia!.CodigoPessoa);
        Assert.Equal(new DateOnly(2026, 3, 1), energia.DataReferencia);
        Assert.Equal(42.5m, energia.Quantidade);
        Assert.Equal(0m, energia.EmissaoCO2);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), energia.CriadoEm);
    }
}
