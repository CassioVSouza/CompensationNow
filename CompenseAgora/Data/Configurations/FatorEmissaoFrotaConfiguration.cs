using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompenseAgora.Data.Configurations
{
    public class FatorEmissaoFrotaConfiguration : IEntityTypeConfiguration<FatorEmissaoFrota>
    {
        public void Configure(EntityTypeBuilder<FatorEmissaoFrota> builder)
        {
            builder.ToTable("FATOR_EMISSAO_FROTA");

            builder.HasKey(b => b.Codigo);

            builder.Property(b => b.CH4).HasPrecision(18, 6);
            builder.Property(b => b.N2O).HasPrecision(18, 6);

            builder.HasOne(b => b.Frota)
                .WithMany()
                .HasForeignKey(b => b.CodigoFrota)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
