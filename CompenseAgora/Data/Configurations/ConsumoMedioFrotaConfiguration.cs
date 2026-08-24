using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompenseAgora.Data.Configurations;

public class ConsumoMedioFrotaConfiguration : IEntityTypeConfiguration<ConsumoMedioFrota>
{
    public void Configure(EntityTypeBuilder<ConsumoMedioFrota> builder)
    {
        builder.ToTable("CONSUMO_MEDIO_FROTA");
        builder.HasKey(c => c.Codigo);

        builder.Property(c => c.UnidadeMedida).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Consumo).HasPrecision(18, 6);

        builder.HasOne(c => c.Frota)
            .WithMany(f => f.ConsumosMedios)
            .HasForeignKey(c => c.CodigoFrota)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
