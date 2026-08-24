using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompenseAgora.Data.Configurations;

public class FrotaConfiguration : IEntityTypeConfiguration<Frota>
{
    public void Configure(EntityTypeBuilder<Frota> builder)
    {
        builder.ToTable("FROTA");
        builder.HasKey(f => f.Codigo);

        builder.Property(f => f.Nome).HasMaxLength(150).IsRequired();

        // Three distinct FKs to COMBUSTIVEL: keep Restrict on all of them so SQL Server
        // never has to resolve multiple cascade paths onto the same fuel row.
        builder.HasOne(f => f.CombustivelPrimario)
            .WithMany(c => c.FrotasComoPrimario)
            .HasForeignKey(f => f.CodigoCombustivelPrimario)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.CombustivelBiogenico)
            .WithMany(c => c.FrotasComoBiogenico)
            .HasForeignKey(f => f.CodigoCombustivelBiogenico)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.CombustivelFossil)
            .WithMany(c => c.FrotasComoFossil)
            .HasForeignKey(f => f.CodigoCombustivelFossil)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
