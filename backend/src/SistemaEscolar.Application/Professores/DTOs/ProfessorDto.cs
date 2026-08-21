namespace SistemaEscolar.Application.Professores.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record ProfessorDto(
    Guid Id,
    string NomeCompleto,
    string Email,
    string Formacao
);
