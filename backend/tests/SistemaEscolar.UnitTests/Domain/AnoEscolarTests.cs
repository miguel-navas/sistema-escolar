using FluentAssertions;
using SistemaEscolar.Domain.AnosEscolares;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class AnoEscolarTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveCriarAnoEscolar()
    {
        var resultado = AnoEscolar.Criar("3º Ano", NivelEnsino.FundamentalAnosIniciais);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Nome.Should().Be("3º Ano");
        resultado.Valor.NivelEnsino.Should().Be(NivelEnsino.FundamentalAnosIniciais);
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is AnoEscolarCriadoEvent);
    }

    [Fact]
    public void Criar_SemNome_DeveFalhar()
    {
        var resultado = AnoEscolar.Criar("", NivelEnsino.EnsinoMedio);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Nome do ano escolar é obrigatório.");
    }
}
