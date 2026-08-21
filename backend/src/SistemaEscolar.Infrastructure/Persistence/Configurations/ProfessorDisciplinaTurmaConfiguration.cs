using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.Vinculos;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class ProfessorDisciplinaTurmaConfiguration : IEntityTypeConfiguration<ProfessorDisciplinaTurma>
{
    public void Configure(EntityTypeBuilder<ProfessorDisciplinaTurma> builder)
    {
        builder.ToTable("professores_disciplinas_turmas");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.ProfessorId)
            .HasColumnName("professor_id")
            .IsRequired();

        builder.Property(v => v.DisciplinaId)
            .HasColumnName("disciplina_id")
            .IsRequired();

        builder.Property(v => v.TurmaId)
            .HasColumnName("turma_id")
            .IsRequired();

        builder.Property(v => v.AnoLetivoId)
            .HasColumnName("ano_letivo_id")
            .IsRequired();

        builder.Property(v => v.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        // Defesa em profundidade: mesma checagem que ExisteVinculoAtivoAsync
        // usa para impedir vínculo duplicado. Não é UNIQUE puro porque um
        // vínculo Encerrado permite recriar o mesmo vínculo depois — por
        // isso o índice não é aplicado no banco (ficaria inconsistente com
        // a regra de negócio "duplicado" = mesma combinação ainda Ativa);
        // um índice normal (não único) basta para acelerar a consulta.
        builder.HasIndex(v => new { v.ProfessorId, v.DisciplinaId, v.TurmaId, v.AnoLetivoId });
    }
}
