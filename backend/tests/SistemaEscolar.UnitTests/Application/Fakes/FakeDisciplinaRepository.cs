using SistemaEscolar.Domain.Disciplinas;

namespace SistemaEscolar.UnitTests.Application.Fakes;

public sealed class FakeDisciplinaRepository : IDisciplinaRepository
{
    private readonly Dictionary<Guid, Disciplina> _disciplinas = new();

    public void Semear(Disciplina disciplina) => _disciplinas[disciplina.Id] = disciplina;

    public Task<Disciplina?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_disciplinas.GetValueOrDefault(id));

    public Task AdicionarAsync(Disciplina disciplina, CancellationToken cancellationToken)
    {
        _disciplinas[disciplina.Id] = disciplina;
        return Task.CompletedTask;
    }
}
