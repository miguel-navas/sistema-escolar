using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Disciplinas;

/// <summary>
/// Disparado quando uma disciplina é criada.
/// </summary>
public sealed record DisciplinaCriadaEvent(Guid DisciplinaId, string Nome, DateTime OcorridoEm) : IDomainEvent;
