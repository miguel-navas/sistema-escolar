using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Vinculos;

/// <summary>
/// Disparado quando um professor é vinculado a uma disciplina/turma/ano
/// letivo.
/// </summary>
public sealed record VinculoProfessorDisciplinaTurmaCriadoEvent(
    Guid VinculoId, Guid ProfessorId, Guid DisciplinaId, Guid TurmaId, DateTime OcorridoEm) : IDomainEvent;

/// <summary>
/// Disparado quando um vínculo é encerrado. A Etapa 3 (Aula) consulta o
/// repositório por vínculos ativos antes de permitir o registro de aula.
/// </summary>
public sealed record VinculoProfessorDisciplinaTurmaEncerradoEvent(Guid VinculoId, DateTime OcorridoEm) : IDomainEvent;
