namespace SistemaEscolar.Application.Alunos.DTOs;

/// <summary>
/// DTO de saída — nunca expor a entidade de domínio diretamente na API.
/// </summary>
public sealed record AlunoDto(
    Guid Id,
    string NomeCompleto,
    DateOnly DataNascimento,
    string? CpfFormatado,
    Guid ResponsavelId,
    string Status,
    bool ConsentimentoBiometricoRegistrado
);
