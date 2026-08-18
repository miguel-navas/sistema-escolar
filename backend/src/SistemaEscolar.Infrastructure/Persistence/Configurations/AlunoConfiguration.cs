using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEscolar.Domain.Alunos;
using SistemaEscolar.Domain.SharedKernel.ValueObjects;

namespace SistemaEscolar.Infrastructure.Persistence.Configurations;

public sealed class AlunoConfiguration : IEntityTypeConfiguration<Aluno>
{
    public void Configure(EntityTypeBuilder<Aluno> builder)
    {
        builder.ToTable("alunos");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.NomeCompleto)
            .HasColumnName("nome_completo")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.DataNascimento)
            .HasColumnName("data_nascimento")
            .IsRequired();

        builder.Property(a => a.ResponsavelId)
            .HasColumnName("responsavel_id")
            .IsRequired();

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.ConsentimentoBiometricoRegistrado)
            .HasColumnName("consentimento_biometrico_registrado")
            .IsRequired();

        builder.Property(a => a.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        // Cpf é Value Object opcional -> mapeado como owned type em coluna própria.
        builder.OwnsOne(a => a.Cpf, cpfBuilder =>
        {
            cpfBuilder.Property(c => c.Numero)
                .HasColumnName("cpf")
                .HasMaxLength(11);

            cpfBuilder.HasIndex(c => c.Numero).IsUnique();
        });

        builder.HasIndex(a => a.ResponsavelId);
    }
}
