namespace SistemaEscolar.Domain.Matriculas;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// </summary>
public interface IMatriculaRepository
{
    Task<Matricula?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExisteMatriculaAtivaAsync(Guid alunoId, Guid anoLetivoId, CancellationToken cancellationToken);
    Task AdicionarAsync(Matricula matricula, CancellationToken cancellationToken);
    void Atualizar(Matricula matricula);
}
