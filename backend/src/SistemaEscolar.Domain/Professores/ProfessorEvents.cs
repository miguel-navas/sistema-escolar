using SistemaEscolar.Domain.Common;

namespace SistemaEscolar.Domain.Professores;

/// <summary>
/// Disparado quando um professor é cadastrado.
/// </summary>
public sealed record ProfessorCadastradoEvent(Guid ProfessorId, string NomeCompleto, DateTime OcorridoEm) : IDomainEvent;
