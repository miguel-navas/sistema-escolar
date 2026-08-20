using FluentAssertions;
using SistemaEscolar.Domain.Disciplinas;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class DisciplinaTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveCriarDisciplina()
    {
        var anoEscolarId = Guid.NewGuid();

        var resultado = Disciplina.Criar("Matemática", 80, anoEscolarId);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Nome.Should().Be("Matemática");
        resultado.Valor.CargaHoraria.Should().Be(80);
        resultado.Valor.AnoEscolarId.Should().Be(anoEscolarId);
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is DisciplinaCriadaEvent);
    }

    [Fact]
    public void Criar_SemNome_DeveFalhar()
    {
        var resultado = Disciplina.Criar("", 80, Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Nome da disciplina é obrigatório.");
    }

    [Fact]
    public void Criar_ComCargaHorariaZeroOuNegativa_DeveFalhar()
    {
        var resultado = Disciplina.Criar("Matemática", 0, Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Carga horária deve ser maior que zero.");
    }

    [Fact]
    public void Criar_SemAnoEscolar_DeveFalhar()
    {
        var resultado = Disciplina.Criar("Matemática", 80, Guid.Empty);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Disciplina precisa estar vinculada a um ano escolar.");
    }
}
