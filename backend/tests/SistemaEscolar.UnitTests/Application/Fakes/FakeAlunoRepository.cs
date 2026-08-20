using SistemaEscolar.Domain.Alunos;

namespace SistemaEscolar.UnitTests.Application.Fakes;

/// <summary>
/// Repositório em memória para testar handlers de Application sem banco.
/// Implementa só o suficiente para os cenários testados.
/// </summary>
public sealed class FakeAlunoRepository : IAlunoRepository
{
    private readonly Dictionary<Guid, Aluno> _alunos = new();

    public void Semear(Aluno aluno) => _alunos[aluno.Id] = aluno;

    public Task<Aluno?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_alunos.GetValueOrDefault(id));

    public Task<bool> ExisteComCpfAsync(string cpfNumero, CancellationToken cancellationToken) =>
        Task.FromResult(_alunos.Values.Any(a => a.Cpf?.Numero == cpfNumero));

    public Task AdicionarAsync(Aluno aluno, CancellationToken cancellationToken)
    {
        _alunos[aluno.Id] = aluno;
        return Task.CompletedTask;
    }

    public void Atualizar(Aluno aluno) => _alunos[aluno.Id] = aluno;
}
