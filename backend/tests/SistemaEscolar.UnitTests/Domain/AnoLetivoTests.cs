using FluentAssertions;
using SistemaEscolar.Domain.AnosLetivos;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class AnoLetivoTests
{
    private static readonly DateOnly DataInicioPadrao = new(2026, 2, 1);
    private static readonly DateOnly DataFimPadrao = new(2026, 12, 15);

    [Fact]
    public void Criar_ComDadosValidos_DeveCriarAnoLetivoPlanejado()
    {
        var resultado = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Status.Should().Be(StatusAnoLetivo.Planejado);
        resultado.Valor.Ano.Should().Be(2026);
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is AnoLetivoCriadoEvent);
    }

    [Fact]
    public void Criar_ComAnoForaDoIntervalo_DeveFalhar()
    {
        var resultado = AnoLetivo.Criar(1999, DataInicioPadrao, DataFimPadrao);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Ano letivo deve estar entre 2000 e 2100.");
    }

    [Fact]
    public void Criar_ComDataFimAntesOuIgualDataInicio_DeveFalhar()
    {
        var resultado = AnoLetivo.Criar(2026, DataInicioPadrao, DataInicioPadrao);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Data de fim deve ser posterior à data de início.");
    }

    [Fact]
    public void Ativar_AnoLetivoPlanejado_DeveAtivarEDispararEvento()
    {
        var anoLetivo = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao).Valor!;

        var resultado = anoLetivo.Ativar();

        resultado.Sucesso.Should().BeTrue();
        anoLetivo.Status.Should().Be(StatusAnoLetivo.Ativo);
        anoLetivo.DomainEvents.Should().ContainSingle(e => e is AnoLetivoAtivadoEvent);
    }

    [Fact]
    public void Ativar_AnoLetivoJaAtivo_DeveFalhar()
    {
        var anoLetivo = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao).Valor!;
        anoLetivo.Ativar();

        var resultado = anoLetivo.Ativar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Ano letivo já está ativo.");
    }

    [Fact]
    public void Ativar_AnoLetivoEncerrado_DeveFalhar()
    {
        var anoLetivo = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao).Valor!;
        anoLetivo.Ativar();
        anoLetivo.Encerrar();

        var resultado = anoLetivo.Ativar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Não é possível ativar um ano letivo encerrado.");
    }

    [Fact]
    public void Encerrar_AnoLetivoAtivo_DeveEncerrarEDispararEvento()
    {
        var anoLetivo = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao).Valor!;
        anoLetivo.Ativar();

        var resultado = anoLetivo.Encerrar();

        resultado.Sucesso.Should().BeTrue();
        anoLetivo.Status.Should().Be(StatusAnoLetivo.Encerrado);
        anoLetivo.DomainEvents.Should().Contain(e => e is AnoLetivoEncerradoEvent);
    }

    [Fact]
    public void Encerrar_AnoLetivoPlanejado_DeveFalhar()
    {
        var anoLetivo = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao).Valor!;

        var resultado = anoLetivo.Encerrar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Não é possível encerrar um ano letivo que ainda não foi ativado.");
    }

    [Fact]
    public void Encerrar_AnoLetivoJaEncerrado_DeveFalhar()
    {
        var anoLetivo = AnoLetivo.Criar(2026, DataInicioPadrao, DataFimPadrao).Valor!;
        anoLetivo.Ativar();
        anoLetivo.Encerrar();

        var resultado = anoLetivo.Encerrar();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Ano letivo já está encerrado.");
    }
}
