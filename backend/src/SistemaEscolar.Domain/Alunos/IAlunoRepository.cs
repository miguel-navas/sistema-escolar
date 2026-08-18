namespace SistemaEscolar.Domain.Alunos;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// Expõe apenas operações com significado de negócio — nunca IQueryable
/// nem detalhes de EF Core vazando para fora desta camada.
/// </summary>
public interface IAlunoRepository
{
    Task<Aluno?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExisteComCpfAsync(string cpfNumero, CancellationToken cancellationToken);
    Task AdicionarAsync(Aluno aluno, CancellationToken cancellationToken);
    void Atualizar(Aluno aluno);
}
