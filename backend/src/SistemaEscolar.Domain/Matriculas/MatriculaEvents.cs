using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Matriculas;

/// <summary>
/// Disparado quando um aluno é matriculado em uma turma. O contexto
/// Financeiro pode ouvir este evento para gerar a cobrança de matrícula.
/// </summary>
public sealed record AlunoMatriculadoEvent(Guid MatriculaId, Guid AlunoId, Guid TurmaId, DateTime OcorridoEm) : IDomainEvent;
