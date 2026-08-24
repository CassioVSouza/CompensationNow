using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompenseAgora.Data.Configurations;

public class EnergiaConfiguration : IEntityTypeConfiguration<Energia>
{
    public void Configure(EntityTypeBuilder<Energia> builder)
    {
        builder.ToTable("ENERGIA");
        builder.HasKey(e => e.Codigo);

        builder.Property(e => e.Quantidade).HasPrecision(18, 6);
        builder.Property(e => e.EmissaoCO2).HasPrecision(18, 6);

        builder.HasOne(e => e.Pessoa)
            .WithMany(p => p.Energias)
            .HasForeignKey(e => e.CodigoPessoa)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
