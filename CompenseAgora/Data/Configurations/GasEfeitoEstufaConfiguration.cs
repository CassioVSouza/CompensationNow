using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompenseAgora.Data.Configurations;

public class GasEfeitoEstufaConfiguration : IEntityTypeConfiguration<GasEfeitoEstufa>
{
    public void Configure(EntityTypeBuilder<GasEfeitoEstufa> builder)
    {
        builder.ToTable("GAS_EFEITO_ESTUFA");
        builder.HasKey(g => g.Codigo);

        builder.Property(g => g.Nome).HasMaxLength(150).IsRequired();
        builder.Property(g => g.Familia).HasMaxLength(150).IsRequired();
        builder.Property(g => g.GWP).HasPrecision(18, 6);
    }
}
