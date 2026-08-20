using FluentAssertions;
using SistemaEscolar.Domain.Turmas;
using Xunit;

namespace SistemaEscolar.UnitTests.Domain;

public sealed class TurmaTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveCriarTurmaComSufixoA()
    {
        var resultado = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 30);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Sufixo.Should().Be('A');
        resultado.Valor.VagasOcupadas.Should().Be(0);
        resultado.Valor.Nome.Should().Be("3º Ano A");
        resultado.Valor.DomainEvents.Should().ContainSingle(e => e is TurmaCriadaEvent);
    }

    [Fact]
    public void Criar_SemNomeBase_DeveFalhar()
    {
        var resultado = Turma.Criar("", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 30);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Nome base da turma é obrigatório.");
    }

    [Fact]
    public void Criar_ComVagasMaximasZeroOuNegativo_DeveFalhar()
    {
        var resultado = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 0);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Número de vagas máximas deve ser maior que zero.");
    }

    [Fact]
    public void OcuparVaga_AteAtingirLimite_DeveDispararTurmaLotadaEvent()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 2).Valor!;

        var primeiraOcupacao = turma.OcuparVaga();
        primeiraOcupacao.Sucesso.Should().BeTrue();
        turma.DomainEvents.Should().NotContain(e => e is TurmaLotadaEvent);

        var segundaOcupacao = turma.OcuparVaga();
        segundaOcupacao.Sucesso.Should().BeTrue();
        turma.VagasOcupadas.Should().Be(2);
        turma.DomainEvents.Should().ContainSingle(e => e is TurmaLotadaEvent);
    }

    [Fact]
    public void OcuparVaga_QuandoJaLotada_DeveFalhar()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 1).Valor!;
        turma.OcuparVaga();

        var resultado = turma.OcuparVaga();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Turma está lotada.");
    }

    [Fact]
    public void PodeReceberNovaMatricula_ComVagaDisponivel_DeveSerVerdadeiro()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 1).Valor!;

        turma.PodeReceberNovaMatricula().Should().BeTrue();
    }

    [Fact]
    public void PodeReceberNovaMatricula_Lotada_DeveSerFalso()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 1).Valor!;
        turma.OcuparVaga();

        turma.PodeReceberNovaMatricula().Should().BeFalse();
    }

    [Fact]
    public void AbrirTurmaIrma_DeveIncrementarSufixo()
    {
        var turmaA = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 30).Valor!;

        var resultado = turmaA.AbrirTurmaIrma();

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Sufixo.Should().Be('B');
        resultado.Valor.Nome.Should().Be("3º Ano B");
        resultado.Valor.VagasMaximas.Should().Be(turmaA.VagasMaximas);
        resultado.Valor.Id.Should().NotBe(turmaA.Id);
    }

    [Fact]
    public void AbrirTurmaIrma_AlemDoLimiteZ_DeveFalhar()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 30).Valor!;

        for (var letra = 'B'; letra <= 'Z'; letra++)
            turma = turma.AbrirTurmaIrma().Valor!;

        var resultado = turma.AbrirTurmaIrma();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Contain("Limite de turmas irmãs");
    }

    [Fact]
    public void LiberarVaga_ComVagaOcupada_DeveDecrementarContador()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 5).Valor!;
        turma.OcuparVaga();

        var resultado = turma.LiberarVaga();

        resultado.Sucesso.Should().BeTrue();
        turma.VagasOcupadas.Should().Be(0);
    }

    [Fact]
    public void LiberarVaga_SemVagaOcupada_DeveFalhar()
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, Guid.NewGuid(), Guid.NewGuid(), 5).Valor!;

        var resultado = turma.LiberarVaga();

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Turma não possui vaga ocupada para liberar.");
    }
}
