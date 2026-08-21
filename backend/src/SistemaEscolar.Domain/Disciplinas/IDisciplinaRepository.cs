namespace SistemaEscolar.Domain.Disciplinas;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// </summary>
public interface IDisciplinaRepository
{
    Task<Disciplina?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task AdicionarAsync(Disciplina disciplina, CancellationToken cancellationToken);
}
