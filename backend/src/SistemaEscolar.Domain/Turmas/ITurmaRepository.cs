namespace SistemaEscolar.Domain.Turmas;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// Expõe apenas operações com significado de negócio.
/// </summary>
public interface ITurmaRepository
{
    Task<Turma?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<Turma>> ObterTurmasDoGrupoAsync(
        string nomeBase, TurnoTurma turno, Guid anoLetivoId, Guid anoEscolarId, CancellationToken cancellationToken);

    Task AdicionarAsync(Turma turma, CancellationToken cancellationToken);
    void Atualizar(Turma turma);
}
