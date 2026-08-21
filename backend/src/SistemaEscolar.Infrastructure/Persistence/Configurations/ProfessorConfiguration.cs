using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.Professores;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class ProfessorConfiguration : IEntityTypeConfiguration<Professor>
{
    public void Configure(EntityTypeBuilder<Professor> builder)
    {
        builder.ToTable("professores");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.NomeCompleto)
            .HasColumnName("nome_completo")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Formacao)
            .HasColumnName("formacao")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        // Email é Value Object obrigatório -> mapeado como owned type em coluna própria.
        builder.OwnsOne(p => p.Email, emailBuilder =>
        {
            emailBuilder.Property(e => e.Endereco)
                .HasColumnName("email")
                .HasMaxLength(200)
                .IsRequired();
        });
    }
}
