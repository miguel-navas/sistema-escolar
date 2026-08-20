using FluentAssertions;
using SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;
using SistemaEscolar.Domain.Alunos;
using SistemaEscolar.Domain.AnosLetivos;
using SistemaEscolar.Domain.Turmas;
using SistemaEscolar.UnitTests.Application.Fakes;
using Xunit;

namespace SistemaEscolar.UnitTests.Application;

public sealed class MatricularAlunoCommandHandlerTests
{
    private readonly FakeAlunoRepository _alunoRepository = new();
    private readonly FakeAnoLetivoRepository _anoLetivoRepository = new();
    private readonly FakeTurmaRepository _turmaRepository = new();
    private readonly FakeMatriculaRepository _matriculaRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly MatricularAlunoCommandHandler _handler;

    public MatricularAlunoCommandHandlerTests()
    {
        _handler = new MatricularAlunoCommandHandler(
            _alunoRepository, _anoLetivoRepository, _turmaRepository, _matriculaRepository, _unitOfWork);
    }

    private Aluno CriarAlunoSemeado()
    {
        var aluno = Aluno.Cadastrar("Maria Silva", new DateOnly(2015, 3, 10), null, Guid.NewGuid()).Valor!;
        _alunoRepository.Semear(aluno);
        return aluno;
    }

    private AnoLetivo CriarAnoLetivoSemeado(bool ativo)
    {
        var anoLetivo = AnoLetivo.Criar(2026, new DateOnly(2026, 2, 1), new DateOnly(2026, 12, 15)).Valor!;
        if (ativo)
            anoLetivo.Ativar();
        _anoLetivoRepository.Semear(anoLetivo);
        return anoLetivo;
    }

    private Turma CriarTurmaSemeada(Guid anoLetivoId, int vagasMaximas = 30)
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, anoLetivoId, Guid.NewGuid(), vagasMaximas).Valor!;
        _turmaRepository.Semear(turma);
        return turma;
    }

    [Fact]
    public async Task Handle_ComAnoLetivoAtivo_DeveMatricular()
    {
        var aluno = CriarAlunoSemeado();
        var anoLetivo = CriarAnoLetivoSemeado(ativo: true);
        var turma = CriarTurmaSemeada(anoLetivo.Id);

        var resultado = await _handler.Handle(
            new MatricularAlunoCommand(aluno.Id, turma.Id, anoLetivo.Id), CancellationToken.None);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Status.Should().Be("Ativa");
    }

    [Fact]
    public async Task Handle_ComAnoLetivoPlanejado_DeveFalhar()
    {
        var aluno = CriarAlunoSemeado();
        var anoLetivo = CriarAnoLetivoSemeado(ativo: false);
        var turma = CriarTurmaSemeada(anoLetivo.Id);

        var resultado = await _handler.Handle(
            new MatricularAlunoCommand(aluno.Id, turma.Id, anoLetivo.Id), CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Só é possível matricular em um ano letivo ativo.");
    }

    [Fact]
    public async Task Handle_ComAnoLetivoEncerrado_DeveFalhar()
    {
        var aluno = CriarAlunoSemeado();
        var anoLetivo = CriarAnoLetivoSemeado(ativo: true);
        anoLetivo.Encerrar();
        var turma = CriarTurmaSemeada(anoLetivo.Id);

        var resultado = await _handler.Handle(
            new MatricularAlunoCommand(aluno.Id, turma.Id, anoLetivo.Id), CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Só é possível matricular em um ano letivo ativo.");
    }

    [Fact]
    public async Task Handle_ComAnoLetivoInexistente_DeveFalhar()
    {
        var aluno = CriarAlunoSemeado();
        var turma = CriarTurmaSemeada(Guid.NewGuid());

        var resultado = await _handler.Handle(
            new MatricularAlunoCommand(aluno.Id, turma.Id, Guid.NewGuid()), CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Ano letivo não encontrado.");
    }

    [Fact]
    public async Task Handle_ComAlunoJaMatriculadoNoAnoLetivo_DeveFalhar()
    {
        var aluno = CriarAlunoSemeado();
        var anoLetivo = CriarAnoLetivoSemeado(ativo: true);
        var turma = CriarTurmaSemeada(anoLetivo.Id);

        await _handler.Handle(new MatricularAlunoCommand(aluno.Id, turma.Id, anoLetivo.Id), CancellationToken.None);
        var resultado = await _handler.Handle(
            new MatricularAlunoCommand(aluno.Id, turma.Id, anoLetivo.Id), CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Aluno já possui matrícula ativa neste ano letivo.");
    }
}
