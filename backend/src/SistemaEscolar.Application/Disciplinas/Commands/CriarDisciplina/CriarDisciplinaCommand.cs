using MediatR;
using SistemaEscolar.Application.Disciplinas.DTOs;

namespace SistemaEscolar.Application.Disciplinas.Commands.CriarDisciplina;

/// <summary>
/// Comando = intenção do usuário. Não contém lógica, só dados de entrada.
/// </summary>
public sealed record CriarDisciplinaCommand(
    string Nome,
    int CargaHoraria,
    Guid AnoEscolarId
) : IRequest<Result<DisciplinaDto>>;

/// <summary>
/// Envelope simples de resultado para a camada de Application (distinto do
/// Result do Domain, para não vazar tipo de domínio até a Api).
/// </summary>
public sealed record Result<T>(bool Sucesso, T? Valor, string? Erro)
{
    public static Result<T> Ok(T valor) => new(true, valor, null);
    public static Result<T> Falha(string erro) => new(false, default, erro);
}
