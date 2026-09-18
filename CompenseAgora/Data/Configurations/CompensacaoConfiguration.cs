using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompenseAgora.Data.Configurations;

public class CompensacaoConfiguration : IEntityTypeConfiguration<Compensacao>
{
    public void Configure(EntityTypeBuilder<Compensacao> builder)
    {
        builder.ToTable("COMPENSACAO");
        builder.HasKey(c => c.Codigo);

        builder.Property(c => c.TipoCompensacao).HasMaxLength(20).IsRequired();
        builder.Property(c => c.QuantidadeCompensada).HasPrecision(18, 6);
        builder.Property(c => c.CriadoEm).IsRequired();

        builder.HasOne(c => c.Pessoa)
            .WithMany(p => p.Compensacoes)
            .HasForeignKey(c => c.CodigoPessoa)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
