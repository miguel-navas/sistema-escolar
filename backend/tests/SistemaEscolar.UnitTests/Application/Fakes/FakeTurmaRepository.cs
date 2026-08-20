using SistemaEscolar.Domain.Turmas;

namespace SistemaEscolar.UnitTests.Application.Fakes;

public sealed class FakeTurmaRepository : ITurmaRepository
{
    private readonly Dictionary<Guid, Turma> _turmas = new();

    public void Semear(Turma turma) => _turmas[turma.Id] = turma;

    public Task<Turma?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_turmas.GetValueOrDefault(id));

    public Task<List<Turma>> ObterTurmasDoGrupoAsync(
        string nomeBase, TurnoTurma turno, Guid anoLetivoId, Guid anoEscolarId, CancellationToken cancellationToken) =>
        Task.FromResult(_turmas.Values
            .Where(t => t.NomeBase == nomeBase
                && t.Turno == turno
                && t.AnoLetivoId == anoLetivoId
                && t.AnoEscolarId == anoEscolarId)
            .OrderBy(t => t.Sufixo)
            .ToList());

    public Task AdicionarAsync(Turma turma, CancellationToken cancellationToken)
    {
        _turmas[turma.Id] = turma;
        return Task.CompletedTask;
    }

    public void Atualizar(Turma turma) => _turmas[turma.Id] = turma;
}
