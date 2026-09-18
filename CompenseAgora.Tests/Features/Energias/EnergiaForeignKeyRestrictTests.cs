using CompenseAgora.Entities;
using CompenseAgora.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Tests.Features.Energias;

/// <summary>
/// ENERGIA.CodigoPessoa uses DeleteBehavior.Restrict (EnergiaConfiguration), so SQL Server (and SQLite,
/// which enforces FKs by default in EF Core) must refuse to delete a Pessoa row that still has Energia
/// children referencing it.
/// </summary>
public class EnergiaForeignKeyRestrictTests
{
    [Fact]
    public async Task Deleting_Pessoa_WithExistingEnergia_ThrowsDbUpdateException()
    {
        using var factory = new SqliteDbContextFactory();
        await using var seedContext = factory.CreateContext();
        var pessoa = TestDataFactory.CreatePessoa(seedContext);
        seedContext.Energias.Add(new Energia
        {
            CodigoPessoa = pessoa.Codigo,
            CriadoEm = new DateOnly(2026, 1, 1),
            DataReferencia = new DateOnly(2026, 1, 1),
            Quantidade = 1m,
            EmissaoCO2 = 0m,
        });
        await seedContext.SaveChangesAsync();

        await using var dbContext = factory.CreateContext();
        var trackedPessoa = await dbContext.Pessoas.FindAsync(pessoa.Codigo);
        dbContext.Pessoas.Remove(trackedPessoa!);

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }
}
