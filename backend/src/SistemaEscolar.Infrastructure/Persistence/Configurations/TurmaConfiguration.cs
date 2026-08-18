using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class TurmaConfiguration : IEntityTypeConfiguration<Turma>
{
    public void Configure(EntityTypeBuilder<Turma> builder)
    {
        builder.ToTable("turmas");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.AnoLetivoId)
            .HasColumnName("ano_letivo_id")
            .IsRequired();

        builder.Property(t => t.AnoEscolarId)
            .HasColumnName("ano_escolar_id")
            .IsRequired();

        builder.Property(t => t.NomeBase)
            .HasColumnName("nome_base")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Sufixo)
            .HasColumnName("sufixo")
            .HasConversion<string>()
            .HasMaxLength(1)
            .IsRequired();

        builder.Property(t => t.Turno)
            .HasColumnName("turno")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.VagasMaximas)
            .HasColumnName("vagas_maximas")
            .IsRequired();

        builder.Property(t => t.VagasOcupadas)
            .HasColumnName("vagas_ocupadas")
            .IsRequired();

        builder.Property(t => t.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        // Nome é propriedade computada em memória (NomeBase + Sufixo), não persistida.
        builder.Ignore(t => t.Nome);

        // Defesa em profundidade: garante no banco que não existem duas
        // turmas com o mesmo sufixo no mesmo grupo (mesma checagem que
        // ObterTurmasDoGrupoAsync usa para procurar vaga).
        builder.HasIndex(t => new { t.NomeBase, t.Turno, t.AnoLetivoId, t.AnoEscolarId, t.Sufixo })
            .IsUnique();
    }
}
