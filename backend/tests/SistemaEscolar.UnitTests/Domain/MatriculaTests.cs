using FluentAssertions;
using SistemaEscolar.Domain.Matriculas;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class MatriculaTests
{
    [Fact]
    public void Matricular_ComDadosValidos_DeveCriarMatriculaAtiva()
    {
        var resultado = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Status.Should().Be(StatusMatricula.Ativa);
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is AlunoMatriculadoEvent);
    }

    [Fact]
    public void Matricular_SemAluno_DeveFalhar()
    {
        var resultado = Matricula.Matricular(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Matrícula precisa estar vinculada a um aluno.");
    }

    [Fact]
    public void Matricular_SemTurma_DeveFalhar()
    {
        var resultado = Matricula.Matricular(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Matrícula precisa estar vinculada a uma turma.");
    }

    [Fact]
    public void Matricular_SemAnoLetivo_DeveFalhar()
    {
        var resultado = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Matrícula precisa estar vinculada a um ano letivo.");
    }

    [Fact]
    public void Trancar_MatriculaAtiva_DeveTrancar()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;

        var resultado = matricula.Trancar();

        resultado.Sucesso.Should().BeTrue();
        matricula.Status.Should().Be(StatusMatricula.Trancada);
    }

    [Fact]
    public void Trancar_MatriculaCancelada_DeveFalhar()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;
        matricula.Cancelar();

        var resultado = matricula.Trancar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Só é possível trancar uma matrícula ativa.");
    }

    [Fact]
    public void Cancelar_MatriculaAtiva_DeveCancelar()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;

        var resultado = matricula.Cancelar();

        resultado.Sucesso.Should().BeTrue();
        matricula.Status.Should().Be(StatusMatricula.Cancelada);
    }

    [Fact]
    public void Cancelar_MatriculaTrancada_DeveCancelar()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;
        matricula.Trancar();

        var resultado = matricula.Cancelar();

        resultado.Sucesso.Should().BeTrue();
        matricula.Status.Should().Be(StatusMatricula.Cancelada);
    }

    [Fact]
    public void Cancelar_MatriculaConcluida_DeveFalhar()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;
        matricula.Concluir();

        var resultado = matricula.Cancelar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Não é possível cancelar uma matrícula já concluída.");
    }

    [Fact]
    public void Concluir_MatriculaAtiva_DeveConcluir()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;

        var resultado = matricula.Concluir();

        resultado.Sucesso.Should().BeTrue();
        matricula.Status.Should().Be(StatusMatricula.Concluida);
    }

    [Fact]
    public void Concluir_MatriculaCancelada_DeveFalhar()
    {
        var matricula = Matricula.Matricular(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Valor!;
        matricula.Cancelar();

        var resultado = matricula.Concluir();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Só é possível concluir uma matrícula ativa.");
    }
}
