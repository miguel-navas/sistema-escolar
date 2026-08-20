namespace SistemaEscolar.Application.Turmas.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record TurmaDto(
    Guid Id,
    string NomeBase,
    char Sufixo,
    string Nome,
    string Turno,
    Guid AnoLetivoId,
    Guid AnoEscolarId,
    int VagasMaximas,
    int VagasOcupadas
);
