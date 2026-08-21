using SistemaEscolar.Domain.Professores;

namespace SistemaEscolar.UnitTests.Application.Fakes;

/// <summary>
/// Repositório em memória para testar handlers de Application sem banco.
/// Implementa só o suficiente para os cenários testados.
/// </summary>
public sealed class FakeProfessorRepository : IProfessorRepository
{
    private readonly Dictionary<Guid, Professor> _professores = new();

    public void Semear(Professor professor) => _professores[professor.Id] = professor;

    public Task<Professor?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_professores.GetValueOrDefault(id));

    public Task AdicionarAsync(Professor professor, CancellationToken cancellationToken)
    {
        _professores[professor.Id] = professor;
        return Task.CompletedTask;
    }
}
