using FluentAssertions;
using SistemaEscolar.Domain.Alunos;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class AlunoTests
{
    [Fact]
    public void Cadastrar_ComDadosValidos_DeveCriarAlunoPreCadastrado()
    {
        var resultado = Aluno.Cadastrar(
            "Maria da Silva",
            new DateOnly(2015, 3, 10),
            cpf: null,
            responsavelId: Guid.NewGuid());

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Status.Should().Be(StatusAluno.PreCadastrado);
        resultado.Valor.ConsentimentoBiometricoRegistrado.Should().BeFalse();
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is AlunoCadastradoEvent);
    }

    [Fact]
    public void Cadastrar_SemNome_DeveFalhar()
    {
        var resultado = Aluno.Cadastrar(
            "",
            new DateOnly(2015, 3, 10),
            cpf: null,
            responsavelId: Guid.NewGuid());

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Nome completo é obrigatório.");
    }

    [Fact]
    public void Cadastrar_SemResponsavel_DeveFalhar()
    {
        var resultado = Aluno.Cadastrar(
            "João Pereira",
            new DateOnly(2015, 3, 10),
            cpf: null,
            responsavelId: Guid.Empty);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Aluno precisa estar vinculado a um responsável.");
    }

    [Fact]
    public void PodeTerPresencaRegistradaPorReconhecimentoFacial_SemConsentimento_DeveSerFalso()
    {
        var aluno = Aluno.Cadastrar("Ana Souza", new DateOnly(2016, 1, 1), null, Guid.NewGuid()).Valor!;
        aluno.Ativar();

        aluno.PodeTerPresencaRegistradaPorReconhecimentoFacial().Should().BeFalse();
    }

    [Fact]
    public void PodeTerPresencaRegistradaPorReconhecimentoFacial_AtivoComConsentimento_DeveSerVerdadeiro()
    {
        var aluno = Aluno.Cadastrar("Ana Souza", new DateOnly(2016, 1, 1), null, Guid.NewGuid()).Valor!;
        aluno.Ativar();
        aluno.RegistrarConsentimentoBiometrico();

        aluno.PodeTerPresencaRegistradaPorReconhecimentoFacial().Should().BeTrue();
    }

    [Fact]
    public void RegistrarConsentimentoBiometrico_Duplicado_DeveFalhar()
    {
        var aluno = Aluno.Cadastrar("Ana Souza", new DateOnly(2016, 1, 1), null, Guid.NewGuid()).Valor!;
        aluno.RegistrarConsentimentoBiometrico();

        var segundaTentativa = aluno.RegistrarConsentimentoBiometrico();

        segundaTentativa.Sucesso.Should().BeFalse();
        segundaTentativa.Erro.Should().Contain("já foi registrado");
    }

    [Theory]
    [InlineData("111.111.111-11")] // todos dígitos iguais -> inválido
    [InlineData("123.456.789-00")] // dígito verificador incorreto
    public void CriarCpf_Invalido_DeveFalhar(string cpfInvalido)
    {
        var resultado = SistemaEscolar.Domain.SharedKernel.ValueObjects.Cpf.Criar(cpfInvalido);

        resultado.Sucesso.Should().BeFalse();
    }
}
