using SistemaEscolar.Domain.Vinculos;

namespace SistemaEscolar.UnitTests.Application.Fakes;

public sealed class FakeProfessorDisciplinaTurmaRepository : IProfessorDisciplinaTurmaRepository
{
    private readonly Dictionary<Guid, ProfessorDisciplinaTurma> _vinculos = new();

    public void Semear(ProfessorDisciplinaTurma vinculo) => _vinculos[vinculo.Id] = vinculo;

    public Task<ProfessorDisciplinaTurma?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_vinculos.GetValueOrDefault(id));

    public Task<bool> ExisteVinculoAtivoAsync(
        Guid professorId, Guid disciplinaId, Guid turmaId, Guid anoLetivoId, CancellationToken cancellationToken) =>
        Task.FromResult(_vinculos.Values.Any(v =>
            v.ProfessorId == professorId
            && v.DisciplinaId == disciplinaId
            && v.TurmaId == turmaId
            && v.AnoLetivoId == anoLetivoId
            && v.Status == StatusVinculo.Ativo));

    public Task AdicionarAsync(ProfessorDisciplinaTurma vinculo, CancellationToken cancellationToken)
    {
        _vinculos[vinculo.Id] = vinculo;
        return Task.CompletedTask;
    }

    public void Atualizar(ProfessorDisciplinaTurma vinculo) => _vinculos[vinculo.Id] = vinculo;
}
