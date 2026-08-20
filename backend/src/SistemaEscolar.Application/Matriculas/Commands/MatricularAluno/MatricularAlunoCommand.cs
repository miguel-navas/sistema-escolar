using MediatR;
using SistemaEscolar.Application.Matriculas.DTOs;

namespace SistemaEscolar.Application.Matriculas.Commands.MatricularAluno;

/// <summary>
/// Comando = intenção do usuário. TurmaId é a turma pretendida; se estiver
/// lotada, o handler procura ou abre automaticamente uma turma-irmã (ver
/// MatricularAlunoCommandHandler).
/// </summary>
public sealed record MatricularAlunoCommand(
    Guid AlunoId,
    Guid TurmaId,
    Guid AnoLetivoId
) : IRequest<Result<MatriculaDto>>;

/// <summary>
/// Envelope simples de resultado para a camada de Application (distinto do
/// Result do Domain, para não vazar tipo de domínio até a Api).
/// </summary>
public sealed record Result<T>(bool Sucesso, T? Valor, string? Erro)
{
    public static Result<T> Ok(T valor) => new(true, valor, null);
    public static Result<T> Falha(string erro) => new(false, default, erro);
}
