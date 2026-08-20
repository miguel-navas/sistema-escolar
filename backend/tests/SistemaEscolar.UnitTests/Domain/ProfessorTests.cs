using FluentAssertions;
using SistemaEscolar.Domain.Professores;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class ProfessorTests
{
    [Fact]
    public void Cadastrar_ComDadosValidos_DeveCriarProfessor()
    {
        var resultado = Professor.Cadastrar("Carla Mendes", "carla.mendes@escola.com", "Licenciatura em Matemática");

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.NomeCompleto.Should().Be("Carla Mendes");
        resultado.Valor.Email.Endereco.Should().Be("carla.mendes@escola.com");
        resultado.Valor.Formacao.Should().Be("Licenciatura em Matemática");
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is ProfessorCadastradoEvent);
    }

    [Fact]
    public void Cadastrar_SemNome_DeveFalhar()
    {
        var resultado = Professor.Cadastrar("", "carla.mendes@escola.com", "Licenciatura em Matemática");

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Nome completo é obrigatório.");
    }

    [Fact]
    public void Cadastrar_ComEmailInvalido_DeveFalhar()
    {
        var resultado = Professor.Cadastrar("Carla Mendes", "nao-e-um-email", "Licenciatura em Matemática");

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("E-mail inválido.");
    }

    [Fact]
    public void Cadastrar_SemFormacao_DeveFalhar()
    {
        var resultado = Professor.Cadastrar("Carla Mendes", "carla.mendes@escola.com", "");

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Formação é obrigatória.");
    }
}
