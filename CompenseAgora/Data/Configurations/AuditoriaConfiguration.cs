using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompenseAgora.Data.Configurations;

public class AuditoriaConfiguration : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        builder.ToTable("AUDITORIA");
        builder.HasKey(a => a.Codigo);

        builder.Property(a => a.DataHora).IsRequired();
        builder.Property(a => a.Acao).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Entidade).HasMaxLength(100);
        builder.Property(a => a.Tela).HasMaxLength(50);

        builder.HasIndex(a => new { a.CodigoPessoa, a.DataHora });

        builder.HasOne(a => a.Pessoa)
            .WithMany()
            .HasForeignKey(a => a.CodigoPessoa)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
