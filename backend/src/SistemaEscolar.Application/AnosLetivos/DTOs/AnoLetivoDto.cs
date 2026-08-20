namespace SistemaEscolar.Application.AnosLetivos.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record AnoLetivoDto(
    Guid Id,
    int Ano,
    DateOnly DataInicio,
    DateOnly DataFim,
    string Status
);
