using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompenseAgora.Data.Configurations;

public class FatorEnergiaConfiguration : IEntityTypeConfiguration<FatorEnergia>
{
    public void Configure(EntityTypeBuilder<FatorEnergia> builder)
    {
        builder.ToTable("FATOR_ENERGIA");
        builder.HasKey(f => f.Codigo);

        builder.Property(f => f.FeSin).HasPrecision(18, 6);
    }
}
