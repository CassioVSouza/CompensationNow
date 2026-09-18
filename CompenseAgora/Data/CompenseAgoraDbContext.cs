using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Data;

public class CompenseAgoraDbContext(DbContextOptions<CompenseAgoraDbContext> options) : DbContext(options)
{
    public DbSet<Pessoa> Pessoas => Set<Pessoa>();
    public DbSet<GasEfeitoEstufa> GasesEfeitoEstufa => Set<GasEfeitoEstufa>();
    public DbSet<Combustivel> Combustiveis => Set<Combustivel>();
    public DbSet<FatorCombustivel> FatoresDoCombustivel => Set<FatorCombustivel>();
    public DbSet<Frota> Frotas => Set<Frota>();
    public DbSet<ConsumoMedioFrota> ConsumosMedioFrota => Set<ConsumoMedioFrota>();
    public DbSet<Viagem> Viagens => Set<Viagem>();
    public DbSet<Energia> Energias => Set<Energia>();
    public DbSet<Compensacao> Compensacoes => Set<Compensacao>();
    public DbSet<FatorComposicaoCombustivel> FatoresComposicaoCombustivel => Set<FatorComposicaoCombustivel>();
    public DbSet<FatorEnergia> FatoresEnergia => Set<FatorEnergia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CompenseAgoraDbContext).Assembly);
    }
}
