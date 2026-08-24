using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompenseAgora.Data.Configurations;

public class FatorComposicaoCombustivelConfiguration : IEntityTypeConfiguration<FatorComposicaoCombustivel>
{
    public void Configure(EntityTypeBuilder<FatorComposicaoCombustivel> builder)
    {
        builder.ToTable("FATOR_COMPOSICAO_COMBUSTIVEL");
        builder.HasKey(f => f.Codigo);

        builder.Property(f => f.PercentualEtanol).HasPrecision(18, 6);
        builder.Property(f => f.PercentualBiodiesel).HasPrecision(18, 6);
    }
}
