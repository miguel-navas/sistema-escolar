namespace SistemaEscolar.Application.Disciplinas.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record DisciplinaDto(
    Guid Id,
    string Nome,
    int CargaHoraria,
    Guid AnoEscolarId
);
