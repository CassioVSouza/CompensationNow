using CompenseAgora.Common.Exceptions;
using CompenseAgora.Entities;
using CompenseAgora.Features.Compensacoes.Commands.DeleteCompensacao;
using CompenseAgora.Features.Compensacoes.Commands.UpdateCompensacao;
using CompenseAgora.Tests.Infrastructure;
using FluentValidation.TestHelper;

namespace CompenseAgora.Tests.Features.Compensacoes;

public class UpdateCompensacaoCommandValidatorTests
{
    private readonly UpdateCompensacaoCommandValidator _validator = new();

    private static UpdateCompensacaoCommand ValidCommand() => new(
        Codigo: 1,
        DataReferencia: new DateOnly(2026, 1, 1),
        TipoCompensacao: "Outro",
        QuantidadeCompensada: 5m);

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
    public void QuantidadeCompensada_Negative_Fails()
    {
        var result = _validator.TestValidate(ValidCommand() with { QuantidadeCompensada = -1m });
        result.ShouldHaveValidationErrorFor(x => x.QuantidadeCompensada);
    }
}

public class UpdateCompensacaoCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesFields()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);

        var compensacao = new Compensacao
        {
            CodigoPessoa = pessoa.Codigo,
            CriadoEm = new DateOnly(2026, 1, 1),
            DataReferencia = new DateOnly(2026, 1, 1),
            TipoCompensacao = "Outro",
            QuantidadeCompensada = 5m,
        };
        seedContext.Compensacoes.Add(compensacao);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new UpdateCompensacaoCommandHandler(dbContext);

        var command = new UpdateCompensacaoCommand(
            compensacao.Codigo, new DateOnly(2026, 5, 5), "Reflorestamento", 20m);
        await handler.Handle(command, CancellationToken.None);

        var updated = await dbContext.Compensacoes.FindAsync(compensacao.Codigo);
        Assert.NotNull(updated);
        Assert.Equal(new DateOnly(2026, 5, 5), updated!.DataReferencia);
        Assert.Equal("Reflorestamento", updated.TipoCompensacao);
        Assert.Equal(20m, updated.QuantidadeCompensada);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new UpdateCompensacaoCommandHandler(dbContext);

        var command = new UpdateCompensacaoCommand(999, new DateOnly(2026, 1, 1), "Outro", 10m);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }
}

public class DeleteCompensacaoCommandValidatorTests
{
    private readonly DeleteCompensacaoCommandValidator _validator = new();

    [Fact]
    public void Codigo_Positive_Passes()
    {
        var result = _validator.TestValidate(new DeleteCompensacaoCommand(1));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Codigo_Zero_Fails()
    {
        var result = _validator.TestValidate(new DeleteCompensacaoCommand(0));
        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }
}

public class DeleteCompensacaoCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemovesCompensacao_WhenItExists()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);

        var compensacao = new Compensacao
        {
            CodigoPessoa = pessoa.Codigo,
            CriadoEm = new DateOnly(2026, 1, 1),
            DataReferencia = new DateOnly(2026, 1, 1),
            TipoCompensacao = "Outro",
            QuantidadeCompensada = 5m,
        };
        seedContext.Compensacoes.Add(compensacao);
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var handler = new DeleteCompensacaoCommandHandler(dbContext);

        await handler.Handle(new DeleteCompensacaoCommand(compensacao.Codigo), CancellationToken.None);

        await using var verifyContext = factory.CreateContext();
        Assert.Null(await verifyContext.Compensacoes.FindAsync(compensacao.Codigo));
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenCodigoDoesNotExist()
    {
        using var factory = new SqliteDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var handler = new DeleteCompensacaoCommandHandler(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new DeleteCompensacaoCommand(999), CancellationToken.None));
    }
}
