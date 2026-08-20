namespace SistemaEscolar.Domain.Vinculos;

/// <summary>
/// Contrato definido pelo Domain, implementado pela Infrastructure.
/// </summary>
public interface IProfessorDisciplinaTurmaRepository
{
    Task<ProfessorDisciplinaTurma?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Usado pela Application para reforçar "sem vínculo duplicado" ao
    /// vincular, e reaproveitado pela Etapa 3 para checar se existe vínculo
    /// ativo antes de permitir o registro de uma Aula.
    /// </summary>
    Task<bool> ExisteVinculoAtivoAsync(
        Guid professorId, Guid disciplinaId, Guid turmaId, Guid anoLetivoId, CancellationToken cancellationToken);

    Task AdicionarAsync(ProfessorDisciplinaTurma vinculo, CancellationToken cancellationToken);
    void Atualizar(ProfessorDisciplinaTurma vinculo);
}
