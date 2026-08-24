using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompenseAgora.Data.Configurations;

public class FatorCombustivelConfiguration : IEntityTypeConfiguration<FatorCombustivel>
{
    public void Configure(EntityTypeBuilder<FatorCombustivel> builder)
    {
        builder.ToTable("FATORES_DO_COMBUSTIVEL");
        builder.HasKey(f => f.Codigo);

        builder.Property(f => f.PoderCalorificoInferior).HasPrecision(18, 6);
        builder.Property(f => f.Densidade).HasPrecision(18, 6);
        builder.Property(f => f.CO2).HasPrecision(18, 6);
        builder.Property(f => f.CH4).HasPrecision(18, 6);
        builder.Property(f => f.N2O).HasPrecision(18, 6);

        builder.HasOne(f => f.Combustivel)
            .WithMany(c => c.FatoresDoCombustivel)
            .HasForeignKey(f => f.CodigoCombustivel)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
