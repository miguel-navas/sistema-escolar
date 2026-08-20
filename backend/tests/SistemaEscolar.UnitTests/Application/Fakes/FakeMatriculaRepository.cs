using SistemaEscolar.Domain.Matriculas;

namespace SistemaEscolar.UnitTests.Application.Fakes;

public sealed class FakeMatriculaRepository : IMatriculaRepository
{
    private readonly Dictionary<Guid, Matricula> _matriculas = new();

    public void Semear(Matricula matricula) => _matriculas[matricula.Id] = matricula;

    public Task<Matricula?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_matriculas.GetValueOrDefault(id));

    public Task<bool> ExisteMatriculaAtivaAsync(Guid alunoId, Guid anoLetivoId, CancellationToken cancellationToken) =>
        Task.FromResult(_matriculas.Values.Any(m =>
            m.AlunoId == alunoId && m.AnoLetivoId == anoLetivoId && m.Status == StatusMatricula.Ativa));

    public Task AdicionarAsync(Matricula matricula, CancellationToken cancellationToken)
    {
        _matriculas[matricula.Id] = matricula;
        return Task.CompletedTask;
    }

    public void Atualizar(Matricula matricula) => _matriculas[matricula.Id] = matricula;
}
