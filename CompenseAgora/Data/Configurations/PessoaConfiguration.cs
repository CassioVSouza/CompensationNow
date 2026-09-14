using CompenseAgora.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompenseAgora.Data.Configurations;

public class PessoaConfiguration : IEntityTypeConfiguration<Pessoa>
{
    public void Configure(EntityTypeBuilder<Pessoa> builder)
    {
        builder.ToTable("PESSOA");
        builder.HasKey(p => p.Codigo);

        builder.Property(p => p.Nome).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Sobrenome).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(320).IsRequired();
        builder.Property(p => p.CognitoSub).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Endereco).HasMaxLength(250);
        builder.Property(p => p.Bairro).HasMaxLength(150);
        builder.Property(p => p.Numero).HasMaxLength(20);
        builder.Property(p => p.Cidade).HasMaxLength(150);
        builder.Property(p => p.Estado).HasMaxLength(100);
        builder.Property(p => p.Pais).HasMaxLength(100);
        builder.Property(p => p.Celular).HasMaxLength(20);

        builder.HasIndex(p => p.Email).IsUnique();
        builder.HasIndex(p => p.CognitoSub).IsUnique();
    }
}
