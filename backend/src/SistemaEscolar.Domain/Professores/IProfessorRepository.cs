namespace SistemaEscolar.Domain.Professores;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// </summary>
public interface IProfessorRepository
{
    Task<Professor?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task AdicionarAsync(Professor professor, CancellationToken cancellationToken);
}
