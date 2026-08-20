namespace SistemaEscolar.Application.AnosEscolares.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record AnoEscolarDto(
    Guid Id,
    string Nome,
    string NivelEnsino
);
