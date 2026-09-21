using CompenseAgora.Features.Compensacoes.Commands.CreateCompensacao;
using CompenseAgora.Tests.Infrastructure;
using FluentValidation.TestHelper;

namespace CompenseAgora.Tests.Features.Compensacoes;

public class CreateCompensacaoCommandValidatorTests
{
    private readonly CreateCompensacaoCommandValidator _validator = new();

    private static CreateCompensacaoCommand ValidCommand() => new(
        CodigoPessoa: 1,
        DataReferencia: new DateOnly(2026, 1, 1),
        TipoCompensacao: "Reflorestamento",
        QuantidadeCompensada: 10m);

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
    public void DataReferencia_Default_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { DataReferencia = default });
        result.ShouldHaveValidationErrorFor(x => x.DataReferencia);
    }

    [Fact]
    public void TipoCompensacao_Empty_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { TipoCompensacao = "" });
        result.ShouldHaveValidationErrorFor(x => x.TipoCompensacao);
    }

    [Fact]
    public void TipoCompensacao_TooLong_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { TipoCompensacao = new string('a', 21) });
        result.ShouldHaveValidationErrorFor(x => x.TipoCompensacao);
    }

    [Fact]
    public void QuantidadeCompensada_Zero_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { QuantidadeCompensada = 0m });
        result.ShouldHaveValidationErrorFor(x => x.QuantidadeCompensada);
    }
}

public class CreateCompensacaoCommandHandlerTests
{
    [Fact]
    public async Task Handle_InsertsCompensacao_AndReturnsGeneratedCodigo()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);

        await using var dbContext = factory.CreateContext();
        var handler = new CreateCompensacaoCommandHandler(dbContext);

        var command = new CreateCompensacaoCommand(
            pessoa.Codigo, new DateOnly(2026, 4, 1), "Créditos de carbono", 12.5m);

        var codigo = await handler.Handle(command, CancellationToken.None);

        Assert.True(codigo > 0);

        var compensacao = await dbContext.Compensacoes.FindAsync(codigo);
        Assert.NotNull(compensacao);
        Assert.Equal(pessoa.Codigo, compensacao!.CodigoPessoa);
        Assert.Equal("Créditos de carbono", compensacao.TipoCompensacao);
        Assert.Equal(12.5m, compensacao.QuantidadeCompensada);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), compensacao.CriadoEm);
    }
}
