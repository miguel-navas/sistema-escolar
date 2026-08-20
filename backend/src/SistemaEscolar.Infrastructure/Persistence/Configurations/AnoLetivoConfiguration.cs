using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.AnosLetivos;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class AnoLetivoConfiguration : IEntityTypeConfiguration<AnoLetivo>
{
    public void Configure(EntityTypeBuilder<AnoLetivo> builder)
    {
        builder.ToTable("anos_letivos");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Ano)
            .HasColumnName("ano")
            .IsRequired();

        builder.Property(a => a.DataInicio)
            .HasColumnName("data_inicio")
            .IsRequired();

        builder.Property(a => a.DataFim)
            .HasColumnName("data_fim")
            .IsRequired();

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();
    }
}
