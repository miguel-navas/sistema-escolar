using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.Disciplinas;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class DisciplinaConfiguration : IEntityTypeConfiguration<Disciplina>
{
    public void Configure(EntityTypeBuilder<Disciplina> builder)
    {
        builder.ToTable("disciplinas");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Nome)
            .HasColumnName("nome")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.CargaHoraria)
            .HasColumnName("carga_horaria")
            .IsRequired();

        builder.Property(d => d.AnoEscolarId)
            .HasColumnName("ano_escolar_id")
            .IsRequired();

        builder.Property(d => d.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.HasIndex(d => d.AnoEscolarId);
    }
}
