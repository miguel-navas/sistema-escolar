using FluentAssertions;
using SistemaEscolar.Domain.Vinculos;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class ProfessorDisciplinaTurmaTests
{
    [Fact]
    public void Vincular_ComDadosValidos_DeveCriarVinculoAtivo()
    {
        var resultado = ProfessorDisciplinaTurma.Vincular(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Status.Should().Be(StatusVinculo.Ativo);
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is VinculoProfessorDisciplinaTurmaCriadoEvent);
    }

    [Fact]
    public void Vincular_SemProfessor_DeveFalhar()
    {
        var resultado = ProfessorDisciplinaTurma.Vincular(
            Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Vínculo precisa estar associado a um professor.");
    }

    [Fact]
    public void Vincular_SemDisciplina_DeveFalhar()
    {
        var resultado = ProfessorDisciplinaTurma.Vincular(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Vínculo precisa estar associado a uma disciplina.");
    }

    [Fact]
    public void Vincular_SemTurma_DeveFalhar()
    {
        var resultado = ProfessorDisciplinaTurma.Vincular(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Vínculo precisa estar associado a uma turma.");
    }

    [Fact]
    public void Vincular_SemAnoLetivo_DeveFalhar()
    {
        var resultado = ProfessorDisciplinaTurma.Vincular(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Vínculo precisa estar associado a um ano letivo.");
    }

    [Fact]
    public void Encerrar_VinculoAtivo_DeveEncerrarEDispararEvento()
    {
        var vinculo = ProfessorDisciplinaTurma.Vincular(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;

        var resultado = vinculo.Encerrar();

        resultado.Sucesso.Should().BeTrue();
        vinculo.Status.Should().Be(StatusVinculo.Encerrado);
        vinculo.DomainEvents.Should().Contain(e => e is VinculoProfessorDisciplinaTurmaEncerradoEvent);
    }

    [Fact]
    public void Encerrar_VinculoJaEncerrado_DeveFalhar()
    {
        var vinculo = ProfessorDisciplinaTurma.Vincular(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;
        vinculo.Encerrar();

        var resultado = vinculo.Encerrar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Vínculo já está encerrado.");
    }
}
