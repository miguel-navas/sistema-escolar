using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.Matriculas;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class MatriculaConfiguration : IEntityTypeConfiguration<Matricula>
{
    public void Configure(EntityTypeBuilder<Matricula> builder)
    {
        builder.ToTable("matriculas");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.AlunoId)
            .HasColumnName("aluno_id")
            .IsRequired();

        builder.Property(m => m.TurmaId)
            .HasColumnName("turma_id")
            .IsRequired();

        builder.Property(m => m.AnoLetivoId)
            .HasColumnName("ano_letivo_id")
            .IsRequired();

        builder.Property(m => m.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.MatriculadoEm)
            .HasColumnName("matriculado_em")
            .IsRequired();

        // Defesa em profundidade: reforça no banco a regra de negócio "no
        // máximo uma matrícula Ativa por aluno/ano letivo" (índice único
        // parcial), a mesma que ExisteMatriculaAtivaAsync já checa na Application.
        builder.HasIndex(m => new { m.AlunoId, m.AnoLetivoId })
            .IsUnique()
            .HasFilter("status = 'Ativa'");
    }
}
