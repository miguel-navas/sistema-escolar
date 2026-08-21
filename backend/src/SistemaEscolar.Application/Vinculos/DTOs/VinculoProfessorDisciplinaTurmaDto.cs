namespace SistemaEscolar.Application.Vinculos.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record VinculoProfessorDisciplinaTurmaDto(
    Guid Id,
    Guid ProfessorId,
    Guid DisciplinaId,
    Guid TurmaId,
    Guid AnoLetivoId,
    string Status,
    DateTime CriadoEm
);
