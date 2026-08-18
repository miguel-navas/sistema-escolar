namespace SistemaEscolar.Application.Matriculas.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record MatriculaDto(
    Guid Id,
    Guid AlunoId,
    Guid TurmaId,
    Guid AnoLetivoId,
    string Status,
    DateTime MatriculadoEm
);
