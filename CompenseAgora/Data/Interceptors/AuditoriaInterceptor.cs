using System.Text.Encodings.Web;
using System.Text.Json;
using CompenseAgora.Common;
using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace CompenseAgora.Data.Interceptors;

/// <summary>
/// Writes an <see cref="Auditoria"/> row for every entity the signed-in user creates, updates or deletes,
/// so individual command handlers don't have to. Field values are captured before the save (original
/// values are gone afterwards), while the audit rows themselves are written after it, because generated
/// keys for newly created rows only exist once the first save completes. Both saves run inside one
/// transaction so an action is never committed without its audit trail. Saves with no signed-in user
/// (e.g. self-registration) are not audited. Only the async SaveChanges path is intercepted, which is
/// the only one the handlers use.
/// </summary>
public class AuditoriaInterceptor(IUsuarioAtual usuarioAtual) : SaveChangesInterceptor
{
    private static readonly HashSet<string> CamposIgnorados = [nameof(Pessoa.CognitoSub)];

    public static readonly JsonSerializerOptions OpcoesJson = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private List<AlteracaoPendente>? _pendentes;
    private int _codigoPessoa;
    private IDbContextTransaction? _transacao;
    private bool _gravandoAuditoria;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_gravandoAuditoria || eventData.Context is not { } context)
        {
            return result;
        }

        var entradas = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not Auditoria
                        && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        if (entradas.Count == 0)
        {
            return result;
        }

        var codigoPessoa = await usuarioAtual.ObterCodigoPessoaAsync();
        if (codigoPessoa is null)
        {
            return result;
        }

        var pendentes = entradas
            .Select(CapturarAlteracao)
            .Where(p => p.Acao != TipoAcaoAuditoria.Alteracao || p.Campos.Count > 0)
            .ToList();
        if (pendentes.Count == 0)
        {
            return result;
        }

        _pendentes = pendentes;
        _codigoPessoa = codigoPessoa.Value;

        if (context.Database.CurrentTransaction is null)
        {
            _transacao = await context.Database.BeginTransactionAsync(cancellationToken);
        }

        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_gravandoAuditoria || _pendentes is null || eventData.Context is not { } context)
        {
            return result;
        }

        var pendentes = _pendentes;
        _pendentes = null;

        var agora = DateTime.UtcNow;
        context.Set<Auditoria>().AddRange(pendentes.Select(p => new Auditoria
        {
            CodigoPessoa = _codigoPessoa,
            DataHora = agora,
            Acao = p.Acao,
            Entidade = p.Entrada.Metadata.ClrType.Name,
            CodigoRegistro = ObterChave(p.Entrada),
            Detalhes = JsonSerializer.Serialize(p.Campos, OpcoesJson),
        }));

        try
        {
            _gravandoAuditoria = true;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await DescartarTransacaoAsync(commit: false);
            throw;
        }
        finally
        {
            _gravandoAuditoria = false;
        }

        await DescartarTransacaoAsync(commit: true);
        return result;
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (_gravandoAuditoria)
        {
            return;
        }

        _pendentes = null;
        await DescartarTransacaoAsync(commit: false);
    }

    private async Task DescartarTransacaoAsync(bool commit)
    {
        if (_transacao is null)
        {
            return;
        }

        var transacao = _transacao;
        _transacao = null;

        if (commit)
        {
            await transacao.CommitAsync();
        }
        else
        {
            await transacao.RollbackAsync();
        }

        await transacao.DisposeAsync();
    }

    private static AlteracaoPendente CapturarAlteracao(EntityEntry entrada)
    {
        var propriedades = entrada.Properties
            .Where(p => !p.Metadata.IsPrimaryKey() && !CamposIgnorados.Contains(p.Metadata.Name));

        List<AlteracaoCampo> campos = entrada.State switch
        {
            EntityState.Added => propriedades
                .Where(p => p.CurrentValue is not null)
                .Select(p => new AlteracaoCampo(p.Metadata.Name, null, Formatar(p.CurrentValue)))
                .ToList(),
            EntityState.Deleted => propriedades
                .Where(p => p.OriginalValue is not null)
                .Select(p => new AlteracaoCampo(p.Metadata.Name, Formatar(p.OriginalValue), null))
                .ToList(),
            _ => propriedades
                .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
                .Select(p => new AlteracaoCampo(p.Metadata.Name, Formatar(p.OriginalValue), Formatar(p.CurrentValue)))
                .ToList(),
        };

        var acao = entrada.State switch
        {
            EntityState.Added => TipoAcaoAuditoria.Criacao,
            EntityState.Deleted => TipoAcaoAuditoria.Exclusao,
            _ => TipoAcaoAuditoria.Alteracao,
        };

        return new AlteracaoPendente(entrada, acao, campos);
    }

    private static int? ObterChave(EntityEntry entrada)
    {
        var chave = entrada.Metadata.FindPrimaryKey()?.Properties;
        return chave is [var propriedade] && entrada.Property(propriedade.Name).CurrentValue is int codigo
            ? codigo
            : null;
    }

    private static string? Formatar(object? valor) => valor switch
    {
        null => null,
        bool b => b ? "Sim" : "Não",
        DateOnly d => d.ToString("dd/MM/yyyy", CulturaBrasileira.PtBr),
        DateTime dt => dt.ToString("dd/MM/yyyy HH:mm", CulturaBrasileira.PtBr),
        decimal m => m.ToString("0.######", CulturaBrasileira.PtBr),
        IFormattable f => f.ToString(null, CulturaBrasileira.PtBr),
        _ => valor.ToString(),
    };

    private sealed record AlteracaoPendente(EntityEntry Entrada, TipoAcaoAuditoria Acao, List<AlteracaoCampo> Campos);
}
