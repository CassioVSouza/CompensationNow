using CompenseAgora.Entities;
using CompenseAgora.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Tests.Features.Auth;

/// <summary>
/// PESSOA has unique indexes on Email and CognitoSub (PessoaConfiguration). EF Core's InMemory provider
/// does not enforce unique indexes, so this is exercised against SQLite in-memory instead.
/// </summary>
public class PessoaUniqueConstraintTests
{
    [Fact]
    public async Task Duplicate_Email_ThrowsOnSaveChanges()
    {
        using var factory = new SqliteDbContextFactory();
        await using var context1 = factory.CreateContext();

        context1.Pessoas.Add(new Pessoa
        {
            Nome = "Ana",
            Sobrenome = "Silva",
            Email = "dup@example.com",
            CognitoSub = "sub-1",
        });
        await context1.SaveChangesAsync();

        await using var context2 = factory.CreateContext();
        context2.Pessoas.Add(new Pessoa
        {
            Nome = "Beatriz",
            Sobrenome = "Souza",
            Email = "dup@example.com",
            CognitoSub = "sub-2",
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context2.SaveChangesAsync());
    }

    [Fact]
    public async Task Duplicate_CognitoSub_ThrowsOnSaveChanges()
    {
        using var factory = new SqliteDbContextFactory();
        await using var context1 = factory.CreateContext();

        context1.Pessoas.Add(new Pessoa
        {
            Nome = "Ana",
            Sobrenome = "Silva",
            Email = "ana@example.com",
            CognitoSub = "dup-sub",
        });
        await context1.SaveChangesAsync();

        await using var context2 = factory.CreateContext();
        context2.Pessoas.Add(new Pessoa
        {
            Nome = "Beatriz",
            Sobrenome = "Souza",
            Email = "beatriz@example.com",
            CognitoSub = "dup-sub",
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context2.SaveChangesAsync());
    }
}
