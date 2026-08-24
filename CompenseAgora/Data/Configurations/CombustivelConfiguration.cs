using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompenseAgora.Data.Configurations;

public class CombustivelConfiguration : IEntityTypeConfiguration<Combustivel>
{
    public void Configure(EntityTypeBuilder<Combustivel> builder)
    {
        builder.ToTable("COMBUSTIVEL");
        builder.HasKey(c => c.Codigo);

        builder.Property(c => c.Nome).HasMaxLength(150).IsRequired();
        builder.Property(c => c.UnidadeMedida).HasMaxLength(20).IsRequired();

        // Self-referencing FKs: a fuel may point to the biogenic/fossil fuel that composes it.
        builder.HasOne(c => c.CombustivelBiogenico)
            .WithMany()
            .HasForeignKey(c => c.CodigoCombustivelBiogenico)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CombustivelFossil)
            .WithMany()
            .HasForeignKey(c => c.CodigoCombustivelFossil)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
