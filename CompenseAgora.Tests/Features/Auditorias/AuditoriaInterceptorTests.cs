using System.Text.Json;
using CompenseAgora.Common;
using CompenseAgora.Data.Interceptors;
using CompenseAgora.Entities;
using CompenseAgora.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Tests.Features.Auditorias;

/// <summary>
/// Exercises AuditoriaInterceptor against real SQLite. Seeding goes through TestDataFactory's synchronous
/// SaveChanges, which the interceptor deliberately doesn't hook, so only the async saves under test are audited.
/// </summary>
public class AuditoriaInterceptorTests
{
    private sealed class UsuarioFixo(int? codigoPessoa) : IUsuarioAtual
    {
        public int? CodigoPessoa { get; set; } = codigoPessoa;
        public Task<int?> ObterCodigoPessoaAsync() => Task.FromResult(CodigoPessoa);
    }

    private static List<AlteracaoCampo> Campos(Auditoria auditoria) =>
        JsonSerializer.Deserialize<List<AlteracaoCampo>>(auditoria.Detalhes!)!;

    private static Compensacao NovaCompensacao(int codigoPessoa) => new()
    {
        CodigoPessoa = codigoPessoa,
        DataReferencia = new DateOnly(2026, 3, 10),
        TipoCompensacao = "Reflorestamento",
        QuantidadeCompensada = 12.5m,
        CriadoEm = new DateOnly(2026, 3, 10),
    };

    [Fact]
    public async Task Create_WritesCriacaoWithGeneratedKeyAndValues()
    {
        var usuario = new UsuarioFixo(null);
        using var factory = new SqliteDbContextFactory(new AuditoriaInterceptor(usuario));
        await using var context = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(context);
        usuario.CodigoPessoa = pessoa.Codigo;

        var compensacao = NovaCompensacao(pessoa.Codigo);
        context.Compensacoes.Add(compensacao);
        await context.SaveChangesAsync();

        var auditoria = await context.Auditorias.AsNoTracking().SingleAsync();
        Assert.Equal(pessoa.Codigo, auditoria.CodigoPessoa);
        Assert.Equal(TipoAcaoAuditoria.Criacao, auditoria.Acao);
        Assert.Equal(nameof(Compensacao), auditoria.Entidade);
        Assert.Equal(compensacao.Codigo, auditoria.CodigoRegistro);
        Assert.Contains(Campos(auditoria), c => c is { Campo: "QuantidadeCompensada", Anterior: null, Novo: "12,5" });
        Assert.DoesNotContain(Campos(auditoria), c => c.Campo == "Codigo");
    }

    [Fact]
    public async Task Update_RecordsOnlyChangedFields_WithBeforeAndAfter()
    {
        var usuario = new UsuarioFixo(null);
        using var factory = new SqliteDbContextFactory(new AuditoriaInterceptor(usuario));
        await using var context = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(context);
        var compensacao = NovaCompensacao(pessoa.Codigo);
        context.Compensacoes.Add(compensacao);
        context.SaveChanges();
        usuario.CodigoPessoa = pessoa.Codigo;

        compensacao.QuantidadeCompensada = 20m;
        await context.SaveChangesAsync();

        var auditoria = await context.Auditorias.AsNoTracking().SingleAsync();
        Assert.Equal(TipoAcaoAuditoria.Alteracao, auditoria.Acao);
        var campo = Assert.Single(Campos(auditoria));
        Assert.Equal(new AlteracaoCampo("QuantidadeCompensada", "12,5", "20"), campo);
    }

    [Fact]
    public async Task Update_WithNoRealChange_IsNotAudited()
    {
        var usuario = new UsuarioFixo(null);
        using var factory = new SqliteDbContextFactory(new AuditoriaInterceptor(usuario));
        await using var context = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(context);
        var compensacao = NovaCompensacao(pessoa.Codigo);
        context.Compensacoes.Add(compensacao);
        context.SaveChanges();
        usuario.CodigoPessoa = pessoa.Codigo;

        context.Compensacoes.Update(compensacao);
        await context.SaveChangesAsync();

        Assert.Empty(await context.Auditorias.ToListAsync());
    }

    [Fact]
    public async Task Delete_WritesExclusaoWithPreviousValues()
    {
        var usuario = new UsuarioFixo(null);
        using var factory = new SqliteDbContextFactory(new AuditoriaInterceptor(usuario));
        await using var context = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(context);
        var compensacao = NovaCompensacao(pessoa.Codigo);
        context.Compensacoes.Add(compensacao);
        context.SaveChanges();
        usuario.CodigoPessoa = pessoa.Codigo;

        context.Compensacoes.Remove(compensacao);
        await context.SaveChangesAsync();

        var auditoria = await context.Auditorias.AsNoTracking().SingleAsync();
        Assert.Equal(TipoAcaoAuditoria.Exclusao, auditoria.Acao);
        Assert.Equal(compensacao.Codigo, auditoria.CodigoRegistro);
        Assert.Contains(Campos(auditoria), c => c is { Campo: "TipoCompensacao", Anterior: "Reflorestamento", Novo: null });
    }

    [Fact]
    public async Task NoSignedInUser_IsNotAudited()
    {
        using var factory = new SqliteDbContextFactory(new AuditoriaInterceptor(new UsuarioFixo(null)));
        await using var context = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(context);

        context.Compensacoes.Add(NovaCompensacao(pessoa.Codigo));
        await context.SaveChangesAsync();

        Assert.Empty(await context.Auditorias.ToListAsync());
    }

    [Fact]
    public async Task FailedSave_RollsBackAndWritesNoAudit()
    {
        var usuario = new UsuarioFixo(null);
        using var factory = new SqliteDbContextFactory(new AuditoriaInterceptor(usuario));
        await using var context = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(context);
        usuario.CodigoPessoa = pessoa.Codigo;

        // FK violation: no Pessoa with this code.
        context.Compensacoes.Add(NovaCompensacao(pessoa.Codigo + 999));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.Null(context.Database.CurrentTransaction);
        await using var verificacao = factory.CreateContext();
        Assert.Empty(await verificacao.Auditorias.ToListAsync());
        Assert.Empty(await verificacao.Compensacoes.ToListAsync());
    }

    [Fact]
    public async Task Pessoa_CognitoSub_IsNeverStored()
    {
        var usuario = new UsuarioFixo(null);
        using var factory = new SqliteDbContextFactory(new AuditoriaInterceptor(usuario));
        await using var context = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(context);
        usuario.CodigoPessoa = pessoa.Codigo;

        context.Pessoas.Add(new Pessoa { Nome = "Bia", Sobrenome = "Souza", Email = "bia@example.com", CognitoSub = "segredo" });
        await context.SaveChangesAsync();

        var auditoria = await context.Auditorias.AsNoTracking().SingleAsync();
        Assert.DoesNotContain("segredo", auditoria.Detalhes);
    }
}
