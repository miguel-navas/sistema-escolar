using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.AnosEscolares;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class AnoEscolarConfiguration : IEntityTypeConfiguration<AnoEscolar>
{
    public void Configure(EntityTypeBuilder<AnoEscolar> builder)
    {
        builder.ToTable("anos_escolares");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Nome)
            .HasColumnName("nome")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.NivelEnsino)
            .HasColumnName("nivel_ensino")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();
    }
}
