using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompenseAgora.Data.Configurations;

public class ViagemConfiguration : IEntityTypeConfiguration<Viagem>
{
    public void Configure(EntityTypeBuilder<Viagem> builder)
    {
        builder.ToTable("VIAGEM");
        builder.HasKey(v => v.Codigo);

        builder.Property(v => v.Consumo).HasPrecision(18, 6);
        builder.Property(v => v.DistanciaKM).HasPrecision(18, 6);
        builder.Property(v => v.EmissaoCO2).HasPrecision(18, 6);

        builder.HasOne(v => v.Pessoa)
            .WithMany(p => p.Viagens)
            .HasForeignKey(v => v.CodigoPessoa)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Frota)
            .WithMany(f => f.Viagens)
            .HasForeignKey(v => v.CodigoFrota)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Combustivel)
            .WithMany(c => c.Viagens)
            .HasForeignKey(v => v.CodigoCombustivel)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
