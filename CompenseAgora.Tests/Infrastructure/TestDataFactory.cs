using CompenseAgora.Data;
using CompenseAgora.Entities;

namespace CompenseAgora.Tests.Infrastructure;

/// <summary>Convenience helpers to seed the minimal related rows CRUD tests for Energia/Viagem need.</summary>
public static class TestDataFactory
{
    public static Pessoa CreatePessoa(CompenseAgoraDbContext dbContext, string email = "ana@example.com")
    {
        var pessoa = new Pessoa
        {
            Nome = "Ana",
            Sobrenome = "Silva",
            Email = email,
            CognitoSub = Guid.NewGuid().ToString(),
        };
        dbContext.Pessoas.Add(pessoa);
        dbContext.SaveChanges();
        return pessoa;
    }

    public static Combustivel CreateCombustivel(CompenseAgoraDbContext dbContext, string nome = "Diesel")
    {
        var combustivel = new Combustivel
        {
            Nome = nome,
            UnidadeMedida = "L",
        };
        dbContext.Combustiveis.Add(combustivel);
        dbContext.SaveChanges();
        return combustivel;
    }

    public static Frota CreateFrota(CompenseAgoraDbContext dbContext, Combustivel? combustivelPrimario = null, string nome = "Frota 1")
    {
        var primario = combustivelPrimario ?? CreateCombustivel(dbContext);

        var frota = new Frota
        {
            Nome = nome,
            CodigoCombustivelPrimario = primario.Codigo,
        };
        dbContext.Frotas.Add(frota);
        dbContext.SaveChanges();
        return frota;
    }
}
