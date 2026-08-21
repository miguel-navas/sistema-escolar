using FluentAssertions;
using SistemaEscolar.Application.Vinculos.Commands.VincularProfessorDisciplinaTurma;
using SistemaEscolar.Domain.Disciplinas;
using SistemaEscolar.Domain.Professores;
using SistemaEscolar.Domain.Turmas;
using SistemaEscolar.UnitTests.Application.Fakes;
using Xunit;

namespace SistemaEscolar.UnitTests.Application;

public sealed class VincularProfessorDisciplinaTurmaCommandHandlerTests
{
    private readonly FakeProfessorRepository _professorRepository = new();
    private readonly FakeDisciplinaRepository _disciplinaRepository = new();
    private readonly FakeTurmaRepository _turmaRepository = new();
    private readonly FakeProfessorDisciplinaTurmaRepository _vinculoRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly VincularProfessorDisciplinaTurmaCommandHandler _handler;

    public VincularProfessorDisciplinaTurmaCommandHandlerTests()
    {
        _handler = new VincularProfessorDisciplinaTurmaCommandHandler(
            _professorRepository, _disciplinaRepository, _turmaRepository, _vinculoRepository, _unitOfWork);
    }

    private Professor CriarProfessorSemeado()
    {
        var professor = Professor.Cadastrar("Carla Mendes", "carla.mendes@escola.com", "Licenciatura em Matemática").Valor!;
        _professorRepository.Semear(professor);
        return professor;
    }

    private Disciplina CriarDisciplinaSemeada(Guid anoEscolarId)
    {
        var disciplina = Disciplina.Criar("Matemática", 80, anoEscolarId).Valor!;
        _disciplinaRepository.Semear(disciplina);
        return disciplina;
    }

    private Turma CriarTurmaSemeada(Guid anoLetivoId, Guid anoEscolarId)
    {
        var turma = Turma.Criar("3º Ano", TurnoTurma.Manha, anoLetivoId, anoEscolarId, 30).Valor!;
        _turmaRepository.Semear(turma);
        return turma;
    }

    [Fact]
    public async Task Handle_ComDadosValidos_DeveVincular()
    {
        var anoEscolarId = Guid.NewGuid();
        var anoLetivoId = Guid.NewGuid();
        var professor = CriarProfessorSemeado();
        var disciplina = CriarDisciplinaSemeada(anoEscolarId);
        var turma = CriarTurmaSemeada(anoLetivoId, anoEscolarId);

        var resultado = await _handler.Handle(
            new VincularProfessorDisciplinaTurmaCommand(professor.Id, disciplina.Id, turma.Id, anoLetivoId),
            CancellationToken.None);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor!.Status.Should().Be("Ativo");
    }

    [Fact]
    public async Task Handle_ComAnoEscolarDivergenteEntreDisciplinaETurma_DeveFalhar()
    {
        var anoLetivoId = Guid.NewGuid();
        var professor = CriarProfessorSemeado();
        var disciplina = CriarDisciplinaSemeada(Guid.NewGuid());
        var turma = CriarTurmaSemeada(anoLetivoId, Guid.NewGuid());

        var resultado = await _handler.Handle(
            new VincularProfessorDisciplinaTurmaCommand(professor.Id, disciplina.Id, turma.Id, anoLetivoId),
            CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Disciplina só pode ser vinculada a uma turma do mesmo ano escolar.");
    }

    [Fact]
    public async Task Handle_ComVinculoIdenticoJaAtivo_DeveFalhar()
    {
        var anoEscolarId = Guid.NewGuid();
        var anoLetivoId = Guid.NewGuid();
        var professor = CriarProfessorSemeado();
        var disciplina = CriarDisciplinaSemeada(anoEscolarId);
        var turma = CriarTurmaSemeada(anoLetivoId, anoEscolarId);

        await _handler.Handle(
            new VincularProfessorDisciplinaTurmaCommand(professor.Id, disciplina.Id, turma.Id, anoLetivoId),
            CancellationToken.None);
        var resultado = await _handler.Handle(
            new VincularProfessorDisciplinaTurmaCommand(professor.Id, disciplina.Id, turma.Id, anoLetivoId),
            CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Professor já possui um vínculo ativo idêntico (mesma disciplina, turma e ano letivo).");
    }

    [Fact]
    public async Task Handle_ComProfessorInexistente_DeveFalhar()
    {
        var anoEscolarId = Guid.NewGuid();
        var anoLetivoId = Guid.NewGuid();
        var disciplina = CriarDisciplinaSemeada(anoEscolarId);
        var turma = CriarTurmaSemeada(anoLetivoId, anoEscolarId);

        var resultado = await _handler.Handle(
            new VincularProfessorDisciplinaTurmaCommand(Guid.NewGuid(), disciplina.Id, turma.Id, anoLetivoId),
            CancellationToken.None);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be("Professor não encontrado.");
    }
}
