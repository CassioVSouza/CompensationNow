using System.Text.Json;
using CompenseAgora.Data.Interceptors;
using CompenseAgora.Entities;
using CompenseAgora.Features.Auditorias.Commands.RegistrarFiltro;
using CompenseAgora.Features.Auditorias.Queries.GetAuditorias;
using CompenseAgora.Tests.Infrastructure;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Tests.Features.Auditorias;

public class GetAuditoriasQueryHandlerTests
{
    private static Auditoria NovaAuditoria(int codigoPessoa, DateTime dataHoraUtc, TipoAcaoAuditoria acao = TipoAcaoAuditoria.Criacao) => new()
    {
        CodigoPessoa = codigoPessoa,
        DataHora = dataHoraUtc,
        Acao = acao,
        Entidade = nameof(Viagem),
        CodigoRegistro = 7,
        Detalhes = JsonSerializer.Serialize(
            new List<AlteracaoCampo> { new("DistanciaKM", null, "120"), new("DataReferencia", null, "01/03/2026") },
            AuditoriaInterceptor.OpcoesJson),
    };

    [Fact]
    public async Task Handle_ReturnsOnlyOwnEntries_NewestFirst_WithPtBrLabels()
    {
        using var factory = new SqliteDbContextFactory();
        await using var context = factory.CreateContext();
        var ana = TestDataFactory.CreatePessoa(context);
        var bia = TestDataFactory.CreatePessoa(context, "bia@example.com");
        context.Auditorias.AddRange(
            NovaAuditoria(ana.Codigo, new DateTime(2026, 3, 1, 12, 0, 0)),
            NovaAuditoria(ana.Codigo, new DateTime(2026, 3, 2, 12, 0, 0), TipoAcaoAuditoria.Exclusao),
            NovaAuditoria(bia.Codigo, new DateTime(2026, 3, 3, 12, 0, 0)));
        await context.SaveChangesAsync();

        var resultado = await new GetAuditoriasQueryHandler(context).Handle(new GetAuditoriasQuery(ana.Codigo), CancellationToken.None);

        Assert.Equal(2, resultado.Total);
        Assert.Equal(["Excluiu viagem nº 7", "Criou viagem nº 7"], resultado.Itens.Select(i => i.Descricao));
        Assert.Equal(["Distância (km)", "Data referência"], resultado.Itens[0].Alteracoes.Select(a => a.Campo));
    }

    [Fact]
    public async Task Handle_FiltersByAcaoAndBrasiliaDateRange_AndPages()
    {
        using var factory = new SqliteDbContextFactory();
        await using var context = factory.CreateContext();
        var ana = TestDataFactory.CreatePessoa(context);
        context.Auditorias.AddRange(
            // 02/03 01:00 UTC is still 01/03 22:00 in Brasília, so it belongs to 01/03.
            NovaAuditoria(ana.Codigo, new DateTime(2026, 3, 2, 1, 0, 0)),
            NovaAuditoria(ana.Codigo, new DateTime(2026, 3, 2, 12, 0, 0)),
            NovaAuditoria(ana.Codigo, new DateTime(2026, 3, 2, 13, 0, 0)),
            NovaAuditoria(ana.Codigo, new DateTime(2026, 3, 2, 14, 0, 0), TipoAcaoAuditoria.Alteracao));
        await context.SaveChangesAsync();
        var handler = new GetAuditoriasQueryHandler(context);
        var dia = new DateOnly(2026, 3, 2);

        var pagina1 = await handler.Handle(
            new GetAuditoriasQuery(ana.Codigo, TipoAcaoAuditoria.Criacao, dia, dia, Pagina: 0, TamanhoPagina: 1), CancellationToken.None);
        var pagina2 = await handler.Handle(
            new GetAuditoriasQuery(ana.Codigo, TipoAcaoAuditoria.Criacao, dia, dia, Pagina: 1, TamanhoPagina: 1), CancellationToken.None);

        Assert.Equal(2, pagina1.Total);
        Assert.Equal(new DateTime(2026, 3, 2, 13, 0, 0), Assert.Single(pagina1.Itens).DataHora);
        Assert.Equal(new DateTime(2026, 3, 2, 12, 0, 0), Assert.Single(pagina2.Itens).DataHora);
    }

    [Fact]
    public void Validator_RejectsInvertedDateRange()
    {
        var result = new GetAuditoriasQueryValidator().TestValidate(
            new GetAuditoriasQuery(1, DataInicio: new DateOnly(2026, 3, 2), DataFim: new DateOnly(2026, 3, 1)));
        result.ShouldHaveValidationErrorFor(x => x.DataFim);
    }
}

public class RegistrarFiltroCommandHandlerTests
{
    [Fact]
    public async Task Handle_StoresFilterEntry_ShownWithScreenAndPeriod()
    {
        using var factory = new SqliteDbContextFactory();
        await using var context = factory.CreateContext();
        var ana = TestDataFactory.CreatePessoa(context);

        await new RegistrarFiltroCommandHandler(context).Handle(
            new RegistrarFiltroCommand(ana.Codigo, "Painel", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
            CancellationToken.None);

        var auditoria = await context.Auditorias.AsNoTracking().SingleAsync();
        Assert.Equal(TipoAcaoAuditoria.Filtro, auditoria.Acao);

        var resultado = await new GetAuditoriasQueryHandler(context).Handle(new GetAuditoriasQuery(ana.Codigo), CancellationToken.None);
        var item = Assert.Single(resultado.Itens);
        Assert.Equal("Aplicou filtro de período na tela Painel: 01/01/2026 a 31/12/2026", item.Descricao);
        Assert.Empty(item.Alteracoes);
    }

    [Fact]
    public void Validator_RequiresTela()
    {
        var result = new RegistrarFiltroCommandValidator().TestValidate(
            new RegistrarFiltroCommand(1, "", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)));
        result.ShouldHaveValidationErrorFor(x => x.Tela);
    }
}
